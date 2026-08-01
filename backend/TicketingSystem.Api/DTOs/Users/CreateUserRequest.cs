using System.ComponentModel.DataAnnotations;

namespace TicketingSystem.Api.DTOs.Users;

public sealed class CreateUserRequest
{
    [Required]
    public string Name { get; init; } = default!;

    [Required]
    public string Email { get; init; } = default!;

    [Required]
    public string Password { get; init; } = default!;

    [Required]
    public byte Role { get; init; }

    public int? CategoryId { get; init; }
}
