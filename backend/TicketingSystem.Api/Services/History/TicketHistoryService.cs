using Microsoft.EntityFrameworkCore;
using TicketingSystem.Api.Data;
using TicketingSystem.Api.DTOs.History;
using TicketingSystem.Api.Enums.History;
using TicketingSystem.Api.Enums.Identity;
using TicketingSystem.Api.Models;

namespace TicketingSystem.Api.Services;

public sealed class TicketHistoryService : ITicketHistoryService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public TicketHistoryService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task LogChangeAsync(
        int ticketId,
        HistoryChangeType changeType,
        string? oldValue,
        string? newValue,
        int changedBy,
        CancellationToken ct)
    {
        var historyEntry = new TicketHistory
        {
            TicketId = ticketId,
            ChangeType = changeType,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedById = changedBy,
            ChangedAt = DateTime.UtcNow
        };

        _db.TicketHistories.Add(historyEntry);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<TicketHistoryDto>> GetHistoryAsync(int ticketId, CancellationToken ct)
    {
        // Check if current user can see internal comments
        var userId = _currentUser.GetUserId();
        var currentUserRole = await _db.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => u.RoleId)
            .FirstOrDefaultAsync(ct);

        var canSeeInternal = currentUserRole is UserRole.Support or UserRole.TeamLeader or UserRole.Admin;

        var rows = await _db.TicketHistories
            .AsNoTracking()
            .Where(h => h.TicketId == ticketId)
            .OrderBy(h => h.ChangedAt) // Chronological order (oldest first)
            .Select(h => new
            {
                h.HistoryId,
                h.TicketId,
                h.ChangeType,
                h.OldValue,
                h.NewValue,
                ChangedByName = _db.Users.Where(u => u.UserId == h.ChangedById).Select(u => u.Name).FirstOrDefault() ?? "Unknown",
                h.ChangedAt
            })
            .ToListAsync(ct);

        // Filter out internal comment history entries for Employees
        if (!canSeeInternal)
        {
            // Extract comment IDs from CommentAdded history entries
            var commentIds = rows
                .Where(r => r.ChangeType == HistoryChangeType.CommentAdded && !string.IsNullOrWhiteSpace(r.NewValue))
                .Select(r =>
                {
                    // NewValue format: "Comment #{commentId}"
                    var match = System.Text.RegularExpressions.Regex.Match(r.NewValue!, @"Comment #(\d+)");
                    return match.Success && int.TryParse(match.Groups[1].Value, out var id) ? (int?)id : null;
                })
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            // Get internal comment IDs
            HashSet<int> internalCommentIds;
            if (commentIds.Count > 0)
            {
                internalCommentIds = await _db.TicketComments
                    .AsNoTracking()
                    .Where(c => commentIds.Contains(c.CommentId) && c.IsInternal)
                    .Select(c => c.CommentId)
                    .ToHashSetAsync(ct);
            }
            else
            {
                internalCommentIds = new HashSet<int>();
            }

            // Filter out history entries for internal comments
            rows = rows
                .Where(r =>
                {
                    if (r.ChangeType != HistoryChangeType.CommentAdded) return true;
                    if (string.IsNullOrWhiteSpace(r.NewValue)) return true;

                    var match = System.Text.RegularExpressions.Regex.Match(r.NewValue!, @"Comment #(\d+)");
                    if (!match.Success || !int.TryParse(match.Groups[1].Value, out var commentId)) return true;

                    return !internalCommentIds.Contains(commentId);
                })
                .ToList();
        }

        // For assignment changes, enrich old/new values with display names.
        var assignmentUserIds = rows
            .Where(r => r.ChangeType == HistoryChangeType.AssignmentChanged)
            .SelectMany(r => new[] { r.OldValue, r.NewValue })
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => int.TryParse(v, out var id) ? (int?)id : null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        Dictionary<int, string> userNamesById;
        if (assignmentUserIds.Count == 0)
        {
            userNamesById = new Dictionary<int, string>();
        }
        else
        {
            userNamesById = await _db.Users
                .AsNoTracking()
                .Where(u => assignmentUserIds.Contains(u.UserId))
                .Select(u => new { u.UserId, u.Name })
                .ToDictionaryAsync(x => x.UserId, x => x.Name, ct);
        }

        return rows
            .Select(r =>
            {
                string? oldDisplay = null;
                string? newDisplay = null;

                if (r.ChangeType == HistoryChangeType.AssignmentChanged)
                {
                    if (!string.IsNullOrWhiteSpace(r.OldValue) && int.TryParse(r.OldValue, out var oldId))
                    {
                        oldDisplay = userNamesById.TryGetValue(oldId, out var n) ? n : $"User #{oldId}";
                    }

                    if (!string.IsNullOrWhiteSpace(r.NewValue) && int.TryParse(r.NewValue, out var newId))
                    {
                        newDisplay = userNamesById.TryGetValue(newId, out var n) ? n : $"User #{newId}";
                    }
                }

                return new TicketHistoryDto(
                    r.HistoryId,
                    r.TicketId,
                    r.ChangeType.ToString(),
                    r.OldValue,
                    r.NewValue,
                    oldDisplay,
                    newDisplay,
                    r.ChangedByName,
                    r.ChangedAt);
            })
            .ToList();
    }
}
