using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketingSystem.Api.Models;

[Index(nameof(TicketId), nameof(CreatedAt))]
[Index(nameof(TicketId))]
public class TicketComment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CommentId { get; set; }

    [Required]
    public int TicketId { get; set; }              // FK -> Tickets(TicketId)

    [Required, MaxLength(2000)]
    public string Content { get; set; } = default!;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public int CreatedById { get; set; }           // FK -> Users(UserId)

    [Required]
    public bool IsInternal { get; set; } = false;
}
