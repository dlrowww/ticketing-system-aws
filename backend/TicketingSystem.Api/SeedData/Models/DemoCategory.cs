namespace TicketingSystem.Api.SeedData.Models;

public class DemoCategory
{
    public int CategoryId { get; set; }
    public string NamePl { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
