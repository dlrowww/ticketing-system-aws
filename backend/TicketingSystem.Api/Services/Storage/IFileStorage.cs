using Npgsql;

namespace TicketingSystem.Api.Services
{
    /// <summary>
    /// Abstraction over binary file storage (DB, disk, object store).
    /// The caller supplies a storageKey (e.g., "db:123" or "uploads/2025/10/123/abc.pdf").
    /// </summary>
    public interface IFileStorage
    {
        /// <summary>
        /// Saves content at the given storageKey. Implementations create or overwrite as needed.
        /// </summary>
        Task SaveAsync(Stream content, string storageKey, string contentType, CancellationToken ct);

        /// <summary>
        /// Saves content at the given storageKey. Implementations create or overwrite as needed.
        /// </summary>
        Task SaveAsync(Stream content, string storageKey, string contentType,
                   CancellationToken ct, NpgsqlConnection? conn, NpgsqlTransaction? tx);

        /// <summary>
        /// Opens a read-only stream for the given storageKey.
        /// </summary>
        Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct);

        /// <summary>
        /// Deletes content at the given storageKey. Returns true if it existed and was deleted.
        /// </summary>
        Task<bool> DeleteAsync(string storageKey, CancellationToken ct);
    }
}