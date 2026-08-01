namespace TicketingSystem.Api.DTOs.Users;

public sealed record UserDetailsDto(
    int UserId,
    string Name,
    string Email,
    byte Role,
    int? CategoryId,
    bool IsActive);
