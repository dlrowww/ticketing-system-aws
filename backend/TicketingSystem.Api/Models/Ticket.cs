using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using TicketingSystem.Api.Enums.Tickets;

namespace TicketingSystem.Api.Models;

[Index(nameof(Status))]
[Index(nameof(CreatedAt))]
[Index(nameof(CreatedById))]
[Index(nameof(AssignedToId))]
public class Ticket
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TicketId { get; set; }

    [Required, MaxLength(120)]
    public string Title { get; set; } = default!;

    [Required, MaxLength(5000)]
    public string Description { get; set; } = default!;

    public int CategoryId { get; set; }
    public TicketPriority Priority { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.New;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public int CreatedById { get; set; }
    public int? AssignedToId { get; set; }

    // Navigation properties
    public Category Category { get; set; } = default!;
    public ICollection<TicketFile> Files { get; set; } = new List<TicketFile>();
}