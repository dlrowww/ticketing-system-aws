namespace TicketingSystem.Api.DTOs.Comments
{
    /// <summary>
    /// Request payload to add a new comment to a ticket.
    /// </summary>
    public sealed class AddCommentRequest
    {
        /// <summary>
        /// Comment content.
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// Whether the comment is internal (visible only to Admin/TeamLeader/Support).
        /// </summary>
        public bool IsInternal { get; set; } = false;
    }
}
