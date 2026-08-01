namespace TicketingSystem.Api.Utils
{    
    /// <summary>
    /// Options for CurrentUserService.
    /// </summary>
    public sealed class CurrentUserOptions
    {
        /// <summary>
        /// Configuration section name used in Program.cs when binding.
        /// </summary>
        public const string SectionName = "User";
        /// <summary>
        /// Used when no authenticated user is present (dev/local/testing).
        /// </summary>
        public int DevUserId { get; init; } = 1;
    }
}