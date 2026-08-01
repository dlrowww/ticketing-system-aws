using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketingSystem.Api.Models;

public class Category
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CategoryId { get; set; }

    [Required, MaxLength(100)]
    public string NamePl { get; set; } = default!;

    [Required, MaxLength(100)]
    public string NameEn { get; set; } = default!;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
