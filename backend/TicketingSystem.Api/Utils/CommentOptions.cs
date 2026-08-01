namespace TicketingSystem.Api.Utils
{
    /// <summary>
    /// Options for Comments.
    /// </summary>
    public sealed class CommentOptions
    {
        /// <summary>
        /// Configuration section name used in Program.cs when binding.
        /// </summary>
        public const string SectionName = "Comments";

        /// <summary> Max comment length. </summary>
        public int MaxLength { get; set; } = 2000;

        /// <summary> Min comment length. </summary>
        public int MinLength { get; set; } = 1; // after Trim
    }
}
