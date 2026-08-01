namespace TicketingSystem.Api.Models
{
    public class TicketFile
    {
        public int TicketFileId { get; set; }
        public int TicketId { get; set; }

        public string OriginalName { get; set; } = default!;
        public string StoredName { get; set; } = default!; // GUID-like, stable across providers
        public string ContentType { get; set; } = "application/octet-stream";
        public long SizeBytes { get; set; }

        // You decided to avoid time zones → use local timestamp type in DB
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int UploaderUserId { get; set; }

        // For disk/S3 this is a relative path; for Postgres storage we can leave it empty
        public string StoragePath { get; set; } = "";

        // integrity/dedup
        public string? ChecksumSha256 { get; set; }

        // Navigation
        public Ticket Ticket { get; set; } = default!;
        public User UploaderUser { get; set; } = default!;
        public TicketFileContent? Content { get; set; } // null for disk/S3 providers
    }
}