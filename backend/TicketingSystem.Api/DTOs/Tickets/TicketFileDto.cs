namespace TicketingSystem.Api.DTOs.Tickets
{
    public record TicketFileDto(
        int TicketFileId,
        int TicketId,
        string OriginalName,
        string ContentType,
        long SizeBytes,
        string? DownloadRoute,
        string? ChecksumSha256,
        string CreatedAt,
        int UploaderUserId
    );
}