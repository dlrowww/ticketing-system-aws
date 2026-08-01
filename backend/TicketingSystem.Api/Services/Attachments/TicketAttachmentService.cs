using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Npgsql;

using System.Globalization;
using System.Net;

using TicketingSystem.Api.Common;
using TicketingSystem.Api.Data;
using TicketingSystem.Api.DTOs.Tickets;
using TicketingSystem.Api.Models;
using TicketingSystem.Api.Utils;
using TicketingSystem.Api.Validators;
using TicketingSystem.Api.Enums.History;

namespace TicketingSystem.Api.Services
{
    /// <summary>
    /// EF implementation for metadata; delegates bytes to IFileStorage (Postgres/disk).
    /// Designed to run inside the same transaction as ticket creation when called from TicketService.
    /// </summary>
    public sealed class TicketAttachmentService : ITicketAttachmentService
    {
        private readonly AppDbContext _db;
        private readonly IFileStorage _storage;
        private readonly FileUploadOptions _opt;
        private readonly IAttachmentValidator _validator;
        private readonly ITicketHistoryService _history;
        private readonly ICurrentUserService _currentUser;

        public TicketAttachmentService(
            AppDbContext db,
            IFileStorage storage,
            IOptions<FileUploadOptions> opt,
            IAttachmentValidator validator,
            ITicketHistoryService history,
            ICurrentUserService currentUser)
        {
            _db = db;
            _storage = storage;
            _opt = opt.Value;
            _validator = validator;
            _history = history;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<TicketFileDto>> AddAsync(
            int ticketId,
            IFormFile[] files,
            int uploaderUserId,
            CancellationToken ct,
            NpgsqlConnection? _ = null,
            NpgsqlTransaction? __ = null)
        {
            if (files is null || files.Length == 0)
                return Array.Empty<TicketFileDto>();

            // Validate ticket exists using the same DbContext (cheap, consistent)
            var ticketExists = await _db.Tickets.AsNoTracking()
                .AnyAsync(t => t.TicketId == ticketId, ct);
            if (!ticketExists)
                throw new AppException(ErrorCodes.TicketNotFound,
                    $"Ticket {ticketId} not found",
                    HttpStatusCode.NotFound);

            _validator.ValidateFiles(files);

            var now = DateTime.UtcNow;

            // If caller already began a transaction (e.g., ticket Create), join it.
            // Otherwise, start our own and commit/rollback here.
            var ownsTx = _db.Database.CurrentTransaction is null;
            IDbContextTransaction? tx = null;
            if (ownsTx)
                tx = await _db.Database.BeginTransactionAsync(ct);

            var inserted = new List<TicketFile>(files.Length);
            var savedKeys = new List<string>(files.Length);

            try
            {
                // Insert metadata rows to get TicketFileId
                foreach (var f in files)
                {
                    inserted.Add(new TicketFile
                    {
                        TicketId       = ticketId,
                        OriginalName   = f.FileName,
                        StoredName     = Guid.NewGuid().ToString("N"),
                        ContentType    = string.IsNullOrWhiteSpace(f.ContentType) ? "application/octet-stream" : f.ContentType,
                        SizeBytes      = f.Length,
                        CreatedAt      = now,
                        UploaderUserId = uploaderUserId,
                        StoragePath    = "",     // not used for DB-backed storage
                        // ChecksumSha256 optional (compute later if needed)
                    });
                }

                _db.TicketFiles.AddRange(inserted);
                await _db.SaveChangesAsync(ct);   // IDs generated here; still inside tx

                // Get the SAME underlying Npgsql connection & transaction
                var efTx = _db.Database.CurrentTransaction ?? tx;
                if (efTx is null)
                    throw new InvalidOperationException("No active EF transaction found for attachment save.");

                var dbTrans = efTx.GetDbTransaction();
                if (dbTrans is not Npgsql.NpgsqlTransaction npgTx)
                    throw new InvalidOperationException("Expected Npgsql transaction for attachment save.");

                var npgConn = (Npgsql.NpgsqlConnection)_db.Database.GetDbConnection();

                // Save blobs inside the SAME transaction; if any save fails -> rollback DB insert
                for (int i = 0; i < files.Length; i++)
                {
                    var f  = files[i];
                    var tf = inserted[i];
                    var storageKey = $"db:{tf.TicketFileId}";

                    await using var stream = f.OpenReadStream();
                    await _storage.SaveAsync(stream, storageKey, tf.ContentType, ct, npgConn, npgTx);
                    savedKeys.Add(storageKey);
                }

                if (ownsTx)
                    await tx!.CommitAsync(ct);

                // Log file additions to history
                foreach (var tf in inserted)
                {
                    await _history.LogChangeAsync(
                        ticketId,
                        HistoryChangeType.FileAdded,
                        null,
                        tf.OriginalName,
                        uploaderUserId,
                        ct);
                }

                // 3) Return DTOs
                var result = inserted
                    .Select(tf => new TicketFileDto(
                        tf.TicketFileId,
                        tf.TicketId,
                        tf.OriginalName,
                        tf.ContentType,
                        tf.SizeBytes,
                        $"/api/tickets/{tf.TicketId}/files/{tf.TicketFileId}",
                        tf.ChecksumSha256,
                        tf.CreatedAt.ToString("o", CultureInfo.InvariantCulture),
                        tf.UploaderUserId
                    ))
                    .ToList();

                return result;
            }
            catch
            {
                // Best-effort cleanup of any blobs saved before the failure
                foreach (var key in savedKeys)
                {
                    try { await _storage.DeleteAsync(key, ct); } catch { /* swallow */ }
                }

                if (ownsTx)
                    await tx!.RollbackAsync(ct);

                throw; // Let global handler produce RFC7807
            }
        }

        public async Task<IReadOnlyList<TicketFileDto>> ListAsync(int ticketId, CancellationToken ct)
        {
            // Ensure ticket exists
            var exists = await _db.Tickets.AsNoTracking()
                .AnyAsync(t => t.TicketId == ticketId, ct);
            if (!exists) 
                throw new AppException(ErrorCodes.TicketNotFound,
                    $"Ticket {ticketId} not found",
                    HttpStatusCode.NotFound);

            var list = await _db.TicketFiles
                .AsNoTracking()
                .Where(tf => tf.TicketId == ticketId)
                .OrderByDescending(tf => tf.CreatedAt)
                .ThenByDescending(tf => tf.TicketFileId)
                .Take(_opt.MaxFiles)
                .Select(tf => new TicketFileDto(
                    tf.TicketFileId,
                    tf.TicketId,
                    tf.OriginalName,
                    tf.ContentType,
                    tf.SizeBytes,
                    $"/api/tickets/{tf.TicketId}/files/{tf.TicketFileId}",
                    tf.ChecksumSha256,
                    tf.CreatedAt.ToUniversalTime().ToString("o"),
                    tf.UploaderUserId
                ))
                .ToListAsync(ct);

            return list;
        }

        public async Task<DownloadFileDto> OpenForDownloadAsync(int ticketId, int ticketFileId, CancellationToken ct)
        {
            // Fetch metadata and validate ownership
            var tf = await _db.TicketFiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TicketFileId == ticketFileId && x.TicketId == ticketId, ct);

            if (tf is null)
                throw new AppException(ErrorCodes.FileNotFound,
                    $"Attachment {ticketFileId} not found for ticket {ticketId}.",
                    HttpStatusCode.NotFound);

            var storageKey = $"db:{tf.TicketFileId}";
            try
            {
                var stream = await _storage.OpenReadAsync(storageKey, ct);
                return new DownloadFileDto(
                    stream,
                    string.IsNullOrWhiteSpace(tf.ContentType) ? "application/octet-stream" : tf.ContentType,
                    string.IsNullOrWhiteSpace(tf.OriginalName) ? $"file-{tf.TicketFileId}" : tf.OriginalName
                );
            }
            catch (FileNotFoundException ex)
            {
                throw new AppException(ErrorCodes.FileContentNotFound,
                    $"Attachment content {ticketFileId} not found for ticket {ticketId}.",
                    HttpStatusCode.NotFound, inner: ex);
            }
        }

        public async Task<bool> DeleteAsync(int ticketFileId, CancellationToken ct)
        {
            var tf = await _db.TicketFiles.FirstOrDefaultAsync(x => x.TicketFileId == ticketFileId, ct);
            if (tf is null)
                throw new AppException(ErrorCodes.FileNotFound,
                    $"Attachment {ticketFileId} not found.",
                    HttpStatusCode.NotFound);

            // Delete bytes first (ignore if missing)
            var storageKey = $"db:{tf.TicketFileId}";
            try
            {
                await _storage.DeleteAsync(storageKey, ct);
            }
            catch (FileNotFoundException)
            {
                // ok – blob already gone
            }
            catch (Exception ex)
            {
                throw new AppException(ErrorCodes.StorageDeleteFailed,
                    "Failed to delete attachment content.",
                    HttpStatusCode.InternalServerError, inner: ex);
            }

            _db.TicketFiles.Remove(tf);
            await _db.SaveChangesAsync(ct);

            // Log file removal to history
            await _history.LogChangeAsync(
                tf.TicketId,
                HistoryChangeType.FileRemoved,
                tf.OriginalName,
                null,
                _currentUser.GetUserId(),
                ct);

            return true;
        }
    }
}