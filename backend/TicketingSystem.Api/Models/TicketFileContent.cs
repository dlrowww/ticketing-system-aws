namespace TicketingSystem.Api.Models
{
    public class TicketFileContent
    {
        public int TicketFileId { get; set; }
        public byte[] Content { get; set; } = Array.Empty<byte>();

        public TicketFile TicketFile { get; set; } = default!;
    }
}