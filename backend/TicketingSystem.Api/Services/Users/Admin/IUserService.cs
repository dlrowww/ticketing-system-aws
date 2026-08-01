using TicketingSystem.Api.Common;
using TicketingSystem.Api.DTOs.Users;

namespace TicketingSystem.Api.Services.Users.Admin;

public interface IUserService
{
    Task<Paged<UserListItemDto>> GetUsersAsync(PageRequest page, string? search, byte? role, byte? category, bool? isActive, CancellationToken ct);
    Task<UserDetailsDto?> GetByIdAsync(int userId, CancellationToken ct);
    Task<UserDetailsDto> CreateAsync(CreateUserRequest req, CancellationToken ct);
    Task<UserDetailsDto> UpdateAsync(int userId, UpdateUserRequest req, CancellationToken ct);
    Task DeleteAsync(int userId, CancellationToken ct);
}
