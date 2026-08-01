using Microsoft.EntityFrameworkCore;

using TicketingSystem.Api.Common;
using TicketingSystem.Api.Data;
using TicketingSystem.Api.DTOs.Comments;
using TicketingSystem.Api.Models;
using TicketingSystem.Api.Validators;
using TicketingSystem.Api.Services.Email;
using TicketingSystem.Api.Enums.History;
using System.Net;
using TicketingSystem.Api.Enums.Identity;

namespace TicketingSystem.Api.Services
{
    public sealed class CommentService : ICommentService
    {
        private readonly AppDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly ICommentValidator _validator;
        private readonly IEmailService _email;
        private readonly ITicketHistoryService _history;
        private readonly ILogger<CommentService> _logger;

        public CommentService(
            AppDbContext db, 
            ICurrentUserService currentUser, 
            ICommentValidator validator,
            IEmailService email,
            ITicketHistoryService history,
            ILogger<CommentService> logger)
        {
            _db = db;
            _currentUser = currentUser;
            _validator = validator;
            _email = email;
            _history = history;
            _logger = logger;
        }

        public async Task<CommentDto> AddAsync(int ticketId, AddCommentRequest req, CancellationToken ct)
        {
            var content = _validator.ValidateAndNormalize(req);

            var exists = await _db.Tickets.AsNoTracking().AnyAsync(t => t.TicketId == ticketId, ct);
            if (!exists)
                throw new AppException(ErrorCodes.TicketNotFound, $"Ticket {ticketId} not found", HttpStatusCode.NotFound);

            var userId = _currentUser.GetUserId();

            var currentUserRole = await _db.Users
                .AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => u.RoleId)
                .FirstOrDefaultAsync(ct);

            var canUseInternal = currentUserRole is UserRole.Support or UserRole.TeamLeader or UserRole.Admin;
            if (req.IsInternal && !canUseInternal)
            {
                throw new AppException(
                    ErrorCodes.CommentInternalNotAllowed,
                    "Only Admin/TeamLeader/Support can create internal comments.",
                    HttpStatusCode.Forbidden);
            }

            var entity = new TicketComment
            {
                TicketId    = ticketId,
                Content     = content,
                CreatedAt   = DateTime.UtcNow,
                CreatedById = userId,
                IsInternal  = req.IsInternal
            };

            _db.TicketComments.Add(entity);
            await _db.SaveChangesAsync(ct); // CommentId generated

            // Log comment addition to history
            await _history.LogChangeAsync(
                ticketId,
                HistoryChangeType.CommentAdded,
                null,
                $"Comment #{entity.CommentId}",
                userId,
                ct);

            // Send email notification ONLY for public comments (fire-and-forget)
            if (!entity.IsInternal)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _email.SendCommentAddedAsync(ticketId, entity.CommentId, userId, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send CommentAdded email for ticket {TicketId}", ticketId);
                    }
                });
            }

            var author = await _db.Users
                .AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => new { u.Name, u.RoleId })
                .FirstOrDefaultAsync(ct);

            return new CommentDto(
                commentId:   entity.CommentId,
                ticketId:    entity.TicketId,
                content:     entity.Content,
                createdAt:   entity.CreatedAt,
                createdById: entity.CreatedById,
                createdByName: author?.Name,
                createdByRoleId: author?.RoleId ?? UserRole.Employee,
                isInternal: entity.IsInternal
            );
        }

        public async Task<IReadOnlyList<CommentDto>> ListAsync(int ticketId, CancellationToken ct)
        {
            var userId = _currentUser.GetUserId();
            var currentUserRole = await _db.Users
                .AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => u.RoleId)
                .FirstOrDefaultAsync(ct);

            var canSeeInternal = currentUserRole is UserRole.Support or UserRole.TeamLeader or UserRole.Admin;

            var query = _db.TicketComments
                .AsNoTracking()
                .Where(c => c.TicketId == ticketId);

            if (!canSeeInternal)
            {
                query = query.Where(c => !c.IsInternal);
            }

            return await query
                .OrderBy(c => c.CreatedAt)
                .Select(c => new CommentDto(
                    c.CommentId,
                    c.TicketId,
                    c.Content,
                    c.CreatedAt,
                    c.CreatedById,
                    _db.Users.Where(u => u.UserId == c.CreatedById).Select(u => u.Name).FirstOrDefault(),
                    _db.Users.Where(u => u.UserId == c.CreatedById).Select(u => u.RoleId).FirstOrDefault(),
                    c.IsInternal
                ))
                .ToListAsync(ct);
        }
    }
}