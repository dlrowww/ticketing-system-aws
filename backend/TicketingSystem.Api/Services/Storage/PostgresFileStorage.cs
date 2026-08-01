using Npgsql;
using NpgsqlTypes;

using System.Data;
using System.Net;

using TicketingSystem.Api.Common;

namespace TicketingSystem.Api.Services
{
    /// <summary>
    /// Stores attachment bytes in PostgreSQL table TicketFileContents via ADO.NET (Npgsql).
    /// Storage key format: "db:{TicketFileId}" or just "{TicketFileId}".
    /// </summary>
    public sealed class PostgresFileStorage : IFileStorage
    {
        private readonly string _connString;

        public PostgresFileStorage(IConfiguration cfg)
        {
            // Uses the same connection as EF (DefaultConnection)
            _connString = cfg.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");
        }

        public async Task SaveAsync(Stream content, string storageKey, string contentType,
                            CancellationToken ct, NpgsqlConnection? conn, NpgsqlTransaction? tx)
        {
            var id = ParseId(storageKey);

            byte[] bytes;
            await using (var ms = new MemoryStream())
            {
                await content.CopyToAsync(ms, ct);
                bytes = ms.ToArray();
            }

            const string sql = @"
                INSERT INTO ""TicketFileContents"" (""TicketFileId"", ""Content"")
                VALUES (@id, @content)
                ON CONFLICT (""TicketFileId"")
                DO UPDATE SET ""Content"" = EXCLUDED.""Content"";";

            if (conn is not null && tx is not null)
            {
                // Use caller's connection/transaction (same tx as TicketFiles insert)
                await using var cmd = new NpgsqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@id", NpgsqlTypes.NpgsqlDbType.Integer, id);
                cmd.Parameters.AddWithValue("@content", NpgsqlTypes.NpgsqlDbType.Bytea, bytes);
                await cmd.ExecuteNonQueryAsync(ct);
                return;
            }

            // Fallback: own connection (works when caller is not in a tx)
            await using var own = new NpgsqlConnection(_connString);
            await own.OpenAsync(ct);
            await using (var cmd = new NpgsqlCommand(sql, own))
            {
                cmd.Parameters.AddWithValue("@id", NpgsqlTypes.NpgsqlDbType.Integer, id);
                cmd.Parameters.AddWithValue("@content", NpgsqlTypes.NpgsqlDbType.Bytea, bytes);
                await cmd.ExecuteNonQueryAsync(ct);
            }
        }

        public async Task SaveAsync(Stream content, string storageKey, string contentType, CancellationToken ct)
        {
            var id = ParseId(storageKey);

            // Read the incoming stream into a byte[] (simple & safe for your current size limits).
            // If you want true streaming later, we can switch to Npgsql's stream APIs.
            byte[] bytes;
            await using (var ms = new MemoryStream())
            {
                await content.CopyToAsync(ms, ct);
                bytes = ms.ToArray();
            }

            await using var conn = new NpgsqlConnection(_connString);
            await conn.OpenAsync(ct);

            // Upsert keeps things idempotent if you ever re-save the same id
            const string sql = @"
                INSERT INTO ""TicketFileContents"" (""TicketFileId"", ""Content"")
                VALUES (@id, @content)
                ON CONFLICT (""TicketFileId"")
                DO UPDATE SET ""Content"" = EXCLUDED.""Content"";";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.Add(new NpgsqlParameter("@id", NpgsqlDbType.Integer) { Value = id });
            cmd.Parameters.Add(new NpgsqlParameter("@content", NpgsqlDbType.Bytea) { Value = bytes });

            try
            {
                await cmd.ExecuteNonQueryAsync(ct);
            }
            catch (Exception ex)
            {
                throw new AppException(ErrorCodes.StorageSaveFailed,
                    "Failed to save attachment content.",
                    HttpStatusCode.InternalServerError, inner: ex);
            }
        }

        public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct)
        {
            var id = ParseId(storageKey);

            await using var conn = new NpgsqlConnection(_connString);
            await conn.OpenAsync(ct);

            const string sql = @"SELECT ""Content"" FROM ""TicketFileContents"" WHERE ""TicketFileId"" = @id;";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.Add(new NpgsqlParameter("@id", NpgsqlDbType.Integer) { Value = id });

            // Single row – content column only
            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, ct);
            if (!await reader.ReadAsync(ct))
                throw new FileNotFoundException($"Content not found for {storageKey}.");

            // For now, load into memory (limit is 20 MB, so we are OK);
            // if files get large (more than 50 MB), need to switch to SequentialAccess + GetStream()
            var bytes = (byte[])reader["Content"];
            return new MemoryStream(bytes, writable: false);
        }

        public async Task<bool> DeleteAsync(string storageKey, CancellationToken ct)
        {
            var id = ParseId(storageKey);

            await using var conn = new NpgsqlConnection(_connString);
            await conn.OpenAsync(ct);

            const string sql = @"DELETE FROM ""TicketFileContents"" WHERE ""TicketFileId"" = @id;";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.Add(new NpgsqlParameter("@id", NpgsqlDbType.Integer) { Value = id });

            var affected = await cmd.ExecuteNonQueryAsync(ct);
            if (affected == 0)
                throw new FileNotFoundException($"Content not found for {storageKey}.");

            return true;
        }

        private static int ParseId(string storageKey)
        {
            if (storageKey.StartsWith("db:", StringComparison.OrdinalIgnoreCase))
                storageKey = storageKey.Substring(3);

            if (!int.TryParse(storageKey, out var id) || id <= 0)
                throw new AppException(ErrorCodes.FileContentNotFound,
                    $"Invalid storageKey for PostgresFileStorage: '{storageKey}'",
                    HttpStatusCode.NotFound);

            return id;
        }
    }
}