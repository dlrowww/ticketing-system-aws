namespace TicketingSystem.Api.DTOs.Users;

/// <summary>
/// Represents a user who can be assigned to a ticket.
/// Used by the assignment dropdown in the frontend.
/// </summary>
public sealed record AssignableUserDto(
	int UserId,
	string Name,
	string Email,
	string RoleName,
	int? CategoryId,
	string? CategoryNamePl,
	string? CategoryNameEn
);
