using Microsoft.EntityFrameworkCore;
using System.Net;

using TicketingSystem.Api.Common;
using TicketingSystem.Api.Data;
using TicketingSystem.Api.Enums.Identity;

namespace TicketingSystem.Api.Services.Tickets;

/// <summary>
/// Validates that users can be assigned to tickets based on role, category, and active status.
/// </summary>
public sealed class TicketAssignmentValidator : ITicketAssignmentValidator
{
    private readonly AppDbContext _db;

    public TicketAssignmentValidator(AppDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task ValidateAssignmentAsync(int ticketCategoryId, int userId, CancellationToken ct)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new { u.UserId, u.RoleId, u.CategoryId, u.IsActive })
            .FirstOrDefaultAsync(ct);

        // User must exist
        if (user is null)
            throw new AppException(
                ErrorCodes.UserNotFound,
                $"User {userId} not found.",
                HttpStatusCode.BadRequest);

        // User must be active
        if (!user.IsActive)
            throw new AppException(
                ErrorCodes.UserInactive,
                $"User {userId} is inactive and cannot be assigned.",
                HttpStatusCode.BadRequest);

        // Only Support, TeamLeader, and Admin can be assigned to tickets
        if (user.RoleId is not (UserRole.Support or UserRole.TeamLeader or UserRole.Admin))
            throw new AppException(
                ErrorCodes.InvalidAssigneeRole,
                $"User {userId} has role {user.RoleId} which cannot be assigned to tickets.",
                HttpStatusCode.BadRequest);

        // Support and TeamLeader must match the ticket category
        if (user.RoleId is UserRole.Support or UserRole.TeamLeader)
        {
            if (user.CategoryId != ticketCategoryId)
                throw new AppException(
                    ErrorCodes.AssigneeCategoryMismatch,
                    $"User {userId} is in category {user.CategoryId} but ticket is in category {ticketCategoryId}.",
                    HttpStatusCode.BadRequest);
        }

        // Admin can be assigned to any category (no check needed)
    }
}
