namespace TicketingSystem.Api.Utils
{
    /// <summary>
    /// Options for Ticket.
    /// </summary>
    public sealed class TicketOptions
    {
        /// <summary>
        /// Configuration section name used in Program.cs when binding.
        /// </summary>
        public const string SectionName = "Ticket";

        public int TitleMinLength { get; set; } = 6;
        public int TitleMaxLength { get; set; } = 120;

        public int DescriptionMinLength { get; set; } = 20;
        public int DescriptionMaxLength { get; set; } = 5000;
    }
}
