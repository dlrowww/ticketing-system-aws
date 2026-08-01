namespace TicketingSystem.Api.DTOs;

public sealed record CurrentUserResponse(
    int UserId,
    string Name,
    string Email,
    byte RoleId,
    int? CategoryId
);
