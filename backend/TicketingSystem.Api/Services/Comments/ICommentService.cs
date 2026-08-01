using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using TicketingSystem.Api.DTOs.Comments;

namespace TicketingSystem.Api.Services
{
    public interface ICommentService
    {
        /// <summary>
        /// Adds a new comment to a ticket and returns the created comment DTO.
        /// Throws AppException with a proper error code if validation fails or the ticket doesn't exist.
        /// </summary>
        Task<CommentDto> AddAsync(int ticketId, AddCommentRequest req, CancellationToken ct);

        /// <summary>
        /// Returns comments for a ticket ordered by CreatedAt ascending.
        /// Handy for tests and UI; cheap and consistent across EF/ADO.
        /// </summary>
        Task<IReadOnlyList<CommentDto>> ListAsync(int ticketId, CancellationToken ct);
    }
}