using Microsoft.EntityFrameworkCore;

using TicketingSystem.Api.Data;
using TicketingSystem.Api.Enums.Identity;

namespace TicketingSystem.Api.Services
{
    /// <summary>
    /// Resolves an assignee for a new ticket using EF Core queries.
    /// MVP rule: pick the first available user matching the ticket's Category
    /// and an appropriate role (e.g., Support/TeamLeader). Returns null if none.
    /// </summary>
    public sealed class AssignmentService : IAssignmentService
    {
        private readonly AppDbContext _db;

        public AssignmentService(AppDbContext db) => _db = db;

        /// On ticket create, auto-assign ONLY to the Team Leader in the same category.
        /// If no TL exists, return null (unassigned).
        public async Task<int?> ResolveAssigneeAsync(int categoryId, CancellationToken ct)
        {
            return await _db.Users
                .AsNoTracking()
                .Where(u => u.CategoryId == categoryId
                            && u.RoleId == UserRole.TeamLeader)
                .OrderBy(u => u.UserId)          // deterministic pick if multiple TLs
                .Select(u => (int?)u.UserId)
                .FirstOrDefaultAsync(ct);        // null => Unassigned
        }
    }
}