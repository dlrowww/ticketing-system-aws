using Npgsql;

using TicketingSystem.Api.DTOs.Tickets;

namespace TicketingSystem.Api.Services
{
    /// <summary>
    /// Domain contract for adding/listing/removing ticket attachments.
    /// Provide multiple implementations (EF, ADO) behind this interface.
    /// </summary>
    public interface ITicketAttachmentService
    {
        /// <summary>
        /// Validates files (count/size/type) and attaches them to the ticket.
        /// Saves metadata in DB and bytes via IFileStorage.
        /// </summary>
        Task<IReadOnlyList<TicketFileDto>> AddAsync(
            int ticketId,
            IFormFile[] files,
            int uploaderUserId,
            CancellationToken ct,
            NpgsqlConnection? externalConn = null,
            NpgsqlTransaction? externalTx = null);

        /// <summary>Returns files metadata for a given ticket.</summary>
        Task<IReadOnlyList<TicketFileDto>> ListAsync(int ticketId, CancellationToken ct);

        /// <summary>Deletes a single attachment (metadata + bytes).</summary>
        Task<bool> DeleteAsync(int ticketFileId, CancellationToken ct);

        /// <summary>Validates the file belongs to the ticket and opens a read stream for download.</summary>
        Task<DownloadFileDto> OpenForDownloadAsync(int ticketId, int ticketFileId, CancellationToken ct);
    }
}