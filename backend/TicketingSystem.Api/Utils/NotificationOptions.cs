#nullable enable
using System.ComponentModel.DataAnnotations;

namespace TicketingSystem.Api.Utils
{
    /// <summary>
    /// Configuration switches and basic tuning for notifications.
    /// Bind to the "Notifications" section in appsettings.*.json.
    /// </summary>
    public sealed class NotificationOptions
    {
        public const string SectionName = "Notifications";

        /// <summary>Global on/off for the notification subsystem.</summary>
        public bool Enabled { get; init; } = true;

        /// <summary>Enable/disable email channel (uses IMailSender).</summary>
        public bool EmailEnabled { get; init; } = true;

        /// <summary>Enable/disable in-app feed (DB-backed notifications table).</summary>
        public bool InAppEnabled { get; init; } = true;

        /// <summary>
        /// If true, certain noisy events can be coalesced (e.g., many comments in short time).
        /// Actual batching logic comes later; this flag allows switching at runtime.
        /// </summary>
        public bool BatchingEnabled { get; init; } = false;

        /// <summary>Max retries for transient send failures (email).</summary>
        [Range(0, 10)]
        public int MaxRetries { get; init; } = 2;

        /// <summary>Delay (seconds) between retries, exponential backoff multiplier applied in code.</summary>
        [Range(0, 600)]
        public int RetryDelaySeconds { get; init; } = 10;

        /// <summary>
        /// Optional default culture hint (ordering of PL/EN sections in dual-language emails).
        /// </summary>
        public string DefaultCulture { get; init; } = "pl-PL";
    }
}