namespace TicketingSystem.Api.DTOs.Tickets
{
    public record DownloadFileDto(
        Stream Content,
        string ContentType,
        string OriginalName
    );    
}
