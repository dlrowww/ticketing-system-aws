namespace TicketingSystem.Api.DTOs.Comments
{
    /// <summary>
    /// A ticket comment as exposed by the API.
    /// </summary>
    public sealed class CommentDto
    {
        public int CommentId { get; init; }
        public int TicketId { get; init; }
        public string Content { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public int CreatedById { get; init; }
        public string? CreatedByName { get; init; }
        public TicketingSystem.Api.Enums.Identity.UserRole CreatedByRoleId { get; init; }
        public bool IsInternal { get; init; }

        public CommentDto(int commentId, int ticketId, string content,
            DateTime createdAt, int createdById, string? createdByName, TicketingSystem.Api.Enums.Identity.UserRole createdByRoleId, bool isInternal)
        {
            CommentId = commentId;
            TicketId = ticketId;
            Content = content;
            CreatedAt = createdAt;
            CreatedById = createdById;
            CreatedByName = createdByName;
            CreatedByRoleId = createdByRoleId;
            IsInternal = isInternal;
        }
    }
}
