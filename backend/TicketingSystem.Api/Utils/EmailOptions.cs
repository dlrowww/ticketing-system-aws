#nullable enable
using System.ComponentModel.DataAnnotations;

namespace TicketingSystem.Api.Utils
{
    /// <summary>
    /// Strongly-typed configuration for outbound email.
    /// Bind this to the "Email" section in appsettings.*.json.
    /// </summary>
    public sealed class EmailOptions
    {
        /// <summary>
        /// Configuration section name used in Program.cs when binding.
        /// </summary>
        public const string SectionName = "Email";

        /// <summary>
        /// Quick global switch. If false, the app will skip real sending
        /// (useful for local/dev).
        /// </summary>
        public bool Enabled { get; init; } = true;

        /// <summary>
        /// SMTP host, e.g. "smtp.mailtrap.io" or "smtp.office365.com".
        /// Not required when using pickup directory mode.
        /// </summary>
        public string? Host { get; init; }

        /// <summary>
        /// SMTP port (587 for STARTTLS, 465 for SSL, 25 for plain).
        /// Ignored in pickup mode.
        /// </summary>
        [Range(1, 65535)]
        public int Port { get; init; } = 2525;

        /// <summary>
        /// If true, the client will use SSL/TLS (implicit) or STARTTLS depending on your mail sender implementation.
        /// </summary>
        public bool UseSsl { get; init; } = true;

        /// <summary>
        /// Sender email used in the From header of all system emails.
        /// </summary>
        [EmailAddress]
        [Required(AllowEmptyStrings = false)]
        public string FromAddress { get; init; } = default!;

        /// <summary>
        /// Friendly display name for the sender (e.g., "Ticketing System").
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string FromName { get; init; } = "Ticketing System";

        /// <summary>
        /// SMTP username (often the same as FromAddress). Optional for open relays (not recommended).
        /// </summary>
        public string? Username { get; init; }

        /// <summary>
        /// SMTP password or app-specific token.
        /// Store in user-secrets/env vars; do NOT commit real secrets.
        /// </summary>
        public string? Password { get; init; }

        /// <summary>
        /// If true, emails are written as .eml files to <see cref="PickupDirectoryLocation"/>
        /// instead of being sent over SMTP. Ideal for local dev/hybrid mode.
        /// </summary>
        public bool UsePickupDirectory { get; init; } = false;

        /// <summary>
        /// Folder path for .eml files when <see cref="UsePickupDirectory"/> is true.
        /// Example: "C:\\maildrop" or "./.maildrop".
        /// </summary>
        public string? PickupDirectoryLocation { get; init; }

        /// <summary>
        /// Optional: default language tag for templates (e.g. "pl-PL" or "en-US").
        /// We’ll still send dual-language bodies per TRP, this can control ordering.
        /// </summary>
        public string DefaultCulture { get; init; } = "pl-PL";
    }
}