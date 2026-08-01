namespace TicketingSystem.Api.DTOs.Users;

public sealed class UpdateUserRequest
{
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Password { get; init; }

    public byte? Role { get; init; }
    public int? CategoryId { get; init; }

    public bool? IsActive { get; init; }
}
