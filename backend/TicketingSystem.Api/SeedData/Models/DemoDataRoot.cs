namespace TicketingSystem.Api.SeedData.Models;

public class DemoDataRoot
{
    public List<DemoCategory> Categories { get; set; } = new();
    public List<DemoUser> Users { get; set; } = new();
    public List<DemoTicket> Tickets { get; set; } = new();
}
