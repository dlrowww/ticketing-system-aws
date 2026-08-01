using TicketingSystem.Api.Enums.Identity;

namespace TicketingSystem.Api.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Name { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public UserRole RoleId { get; set; }
        public int? CategoryId { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public Category? Category { get; set; }
    }
}