namespace TicketingSystem.Api.SeedData.Models;

public class DemoComment
{
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; } = false;
    public string AuthorEmail { get; set; } = string.Empty;
    public int DaysAgoFromCreation { get; set; } // Days relative to ticket creation
}
