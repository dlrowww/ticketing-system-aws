namespace TicketingSystem.Api.SeedData.Models;

public class DemoUser
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Admin", "TeamLeader", "Support", "Employee"
    public string? Category { get; set; } // "IT", "Logistics", "Administration" (null for Admin/Employee)
}
