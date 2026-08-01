using Microsoft.EntityFrameworkCore;
using System.Net;

using TicketingSystem.Api.Common;
using TicketingSystem.Api.Data;
using TicketingSystem.Api.DTOs.Users;
using TicketingSystem.Api.Models;
using TicketingSystem.Api.Validators.Users;

namespace TicketingSystem.Api.Services.Users.Admin;

public sealed class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly IUserValidator _validator;
    private readonly ICurrentUserService _currentUser;

    public UserService(AppDbContext db, IUserValidator validator, ICurrentUserService currentUser)
    {
        _db = db;
        _validator = validator;
        _currentUser = currentUser;
    }

    public async Task<Paged<UserListItemDto>> GetUsersAsync(PageRequest page, string? search, byte? role, byte? category, bool? isActive, CancellationToken ct)
    {
        var query = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(u =>
                u.Name.ToLower().Contains(s) ||
                u.Email.ToLower().Contains(s) ||
                u.UserId.ToString() == s);
        }

        if (role.HasValue)
        {
            query = query.Where(u => (byte)u.RoleId == role.Value);
        }

        if (category.HasValue)
        {
            query = query.Where(u => u.CategoryId == category.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        // Apply ordering on entity properties before projecting to DTO.
        // EF Core may fail to translate ordering over custom DTO projections.
        var sort = page.TryGetSort();
        if (sort is not null)
        {
            var field = sort.Field.Trim();
            var desc = sort.Direction == SortDirection.Desc;

            query = field.ToLowerInvariant() switch
            {
                "userid" or "id" => desc ? query.OrderByDescending(u => u.UserId) : query.OrderBy(u => u.UserId),
                "name" => desc ? query.OrderByDescending(u => u.Name) : query.OrderBy(u => u.Name),
                "email" => desc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                "role" or "roleid" => desc ? query.OrderByDescending(u => u.RoleId) : query.OrderBy(u => u.RoleId),
                "category" or "categoryid" => desc ? query.OrderByDescending(u => u.CategoryId) : query.OrderBy(u => u.CategoryId),
                "isactive" => desc ? query.OrderByDescending(u => u.IsActive) : query.OrderBy(u => u.IsActive),
                _ => desc ? query.OrderByDescending(u => u.UserId) : query.OrderBy(u => u.UserId)
            };
        }

        var projected = query.Select(u => new UserListItemDto(
            u.UserId,
            u.Name,
            u.Email,
            (byte)u.RoleId,
            u.CategoryId,
            u.IsActive));

        // Sorting already applied above (on entity). Keep paging helper from applying sort on DTO.
        var paging = new PageRequest { Page = page.Page, Size = page.Size, Sort = null };
        return await projected.ToPagedAsync(paging, ct);
    }

    public async Task<UserDetailsDto?> GetByIdAsync(int userId, CancellationToken ct)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new UserDetailsDto(
                u.UserId,
                u.Name,
                u.Email,
                (byte)u.RoleId,
                u.CategoryId,
                u.IsActive))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<UserDetailsDto> CreateAsync(CreateUserRequest req, CancellationToken ct)
    {
        var normalized = _validator.ValidateAndNormalize(req);

        var exists = await _db.Users.AnyAsync(u => u.Email.ToLower() == normalized.Email, ct);
        if (exists)
        {
            throw new AppException(ErrorCodes.UserEmailAlreadyExists, "Email already exists", HttpStatusCode.Conflict);
        }

        var user = new User
        {
            Name = normalized.Name,
            Email = normalized.Email,
            PasswordHash = PasswordHasher.Hash(normalized.Password),
            RoleId = (Enums.Identity.UserRole)normalized.Role,
            CategoryId = normalized.CategoryId,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return new UserDetailsDto(user.UserId, user.Name, user.Email, (byte)user.RoleId, user.CategoryId, user.IsActive);
    }

    public async Task<UserDetailsDto> UpdateAsync(int userId, UpdateUserRequest req, CancellationToken ct)
    {
        var normalized = _validator.ValidateAndNormalize(req);

        var user = await _db.Users.SingleOrDefaultAsync(u => u.UserId == userId, ct);
        if (user is null)
        {
            throw new AppException(ErrorCodes.UserNotFound, "User not found", HttpStatusCode.NotFound);
        }

        // Prevent self-deactivation
        if (normalized.IsActive is false)
        {
            var currentId = _currentUser.GetUserId();
            if (currentId == userId)
            {
                throw new AppException(ErrorCodes.UserCannotDeactivateSelf, "Cannot deactivate own account", HttpStatusCode.Conflict);
            }

            // Check if user has assigned tickets
            var hasAssignedTickets = await _db.Tickets
                .AsNoTracking()
                .AnyAsync(t => t.AssignedToId == userId && t.Status != Enums.Tickets.TicketStatus.Resolved && t.Status != Enums.Tickets.TicketStatus.Cancelled, ct);

            if (hasAssignedTickets)
            {
                throw new AppException(
                    ErrorCodes.UserHasAssignedTickets,
                    "Cannot deactivate user with active assigned tickets. Please reassign tickets first.",
                    HttpStatusCode.Conflict);
            }
        }

        if (normalized.Email is not null)
        {
            var emailExists = await _db.Users.AnyAsync(u => u.UserId != userId && u.Email.ToLower() == normalized.Email, ct);
            if (emailExists)
            {
                throw new AppException(ErrorCodes.UserEmailAlreadyExists, "Email already exists", HttpStatusCode.Conflict);
            }
            user.Email = normalized.Email;
        }

        if (normalized.Name is not null)
        {
            user.Name = normalized.Name;
        }

        if (normalized.Password is not null)
        {
            user.PasswordHash = PasswordHasher.Hash(normalized.Password);
        }

        if (normalized.Role is not null)
        {
            user.RoleId = (Enums.Identity.UserRole)normalized.Role.Value;
        }

        if (normalized.CategoryId is not null)
        {
            user.CategoryId = normalized.CategoryId.Value;
        }

        if (normalized.IsActive is not null)
        {
            user.IsActive = normalized.IsActive.Value;
        }

        await _db.SaveChangesAsync(ct);

        return new UserDetailsDto(user.UserId, user.Name, user.Email, (byte)user.RoleId, user.CategoryId, user.IsActive);
    }

    public async Task DeleteAsync(int userId, CancellationToken ct)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.UserId == userId, ct);
        if (user is null)
        {
            throw new AppException(ErrorCodes.UserNotFound, "User not found", HttpStatusCode.NotFound);
        }

        var currentId = _currentUser.GetUserId();
        if (currentId == userId)
        {
            throw new AppException(ErrorCodes.UserCannotDeactivateSelf, "Cannot deactivate own account", HttpStatusCode.Conflict);
        }

        // Check if user has assigned tickets
        var hasAssignedTickets = await _db.Tickets
            .AsNoTracking()
            .AnyAsync(t => t.AssignedToId == userId && t.Status != Enums.Tickets.TicketStatus.Resolved && t.Status != Enums.Tickets.TicketStatus.Cancelled, ct);

        if (hasAssignedTickets)
        {
            throw new AppException(
                ErrorCodes.UserHasAssignedTickets,
                "Cannot delete user with active assigned tickets. Please reassign tickets first.",
                HttpStatusCode.Conflict);
        }

        user.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }
}
