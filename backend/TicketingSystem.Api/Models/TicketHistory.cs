using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TicketingSystem.Api.Enums.History;

namespace TicketingSystem.Api.Models;

[Index(nameof(TicketId), nameof(ChangedAt))]
[Index(nameof(TicketId))]
public class TicketHistory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int HistoryId { get; set; }

    [Required]
    public int TicketId { get; set; }              // FK -> Tickets(TicketId)

    [Required]
    public HistoryChangeType ChangeType { get; set; }  // Enum: TicketCreated, StatusChanged, etc.

    [MaxLength(500)]
    public string? OldValue { get; set; }

    [MaxLength(500)]
    public string? NewValue { get; set; }

    [Required]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public int ChangedById { get; set; }           // FK -> Users(UserId)
}
