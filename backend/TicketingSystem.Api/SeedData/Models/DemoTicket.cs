namespace TicketingSystem.Api.SeedData.Models;

public class DemoTicket
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "IT", "Logistics", "Administration"
    public string Priority { get; set; } = string.Empty; // "Low", "Medium", "High", "Critical"
    public string Status { get; set; } = string.Empty; // "New", "Open", "InProcess", "Resolved", etc.
    public int CreatedDaysAgo { get; set; } // How many days in the past was this created
    public int? ResolvedDaysAgo { get; set; } // When was it resolved (for Resolved tickets)
    public string CreatorEmail { get; set; } = string.Empty;
    public string? AssigneeEmail { get; set; } // Null for unassigned tickets
    public List<string> Attachments { get; set; } = new(); // Filenames from docs/AttachementFiles/
    public List<DemoComment> Comments { get; set; } = new();
    
    // Special workflow ticket (for email notification testing)
    public bool IsComplexWorkflowTest { get; set; } = false;
    public string? InitialAssigneeEmail { get; set; } // For reassignment workflow
    public string? InitialPriority { get; set; } // For priority escalation workflow
}
