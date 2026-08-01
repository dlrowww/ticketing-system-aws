using System.ComponentModel.DataAnnotations;

namespace TicketingSystem.Api.Infrastructure.Email;

/// <summary>
/// Configuration options for email sending.
/// Validates at startup to ensure all required settings are provided.
/// </summary>
public class EmailOptions
{
    /// <summary>
    /// SMTP server host address.
    /// </summary>
    [Required(ErrorMessage = "SMTP Host is required")]
    public string SmtpHost { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port (typically 25, 587, or 465).
    /// </summary>
    [Range(1, 65535, ErrorMessage = "SMTP Port must be between 1 and 65535")]
    public int SmtpPort { get; set; }

    /// <summary>
    /// Whether to use SSL/TLS for SMTP connection.
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// SMTP username for authentication (if required by server).
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// SMTP password for authentication (if required by server).
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Email address to send emails from.
    /// </summary>
    [Required(ErrorMessage = "FromAddress is required")]
    [EmailAddress(ErrorMessage = "FromAddress must be a valid email address")]
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// Display name for sender (bilingual: "IronPack - System Zgłoszeń / Ticketing System").
    /// </summary>
    [Required(ErrorMessage = "FromName is required")]
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// Base URL of the frontend application (used to construct ticket links).
    /// </summary>
    [Required(ErrorMessage = "BaseUrl is required")]
    [Url(ErrorMessage = "BaseUrl must be a valid URL")]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Path to email templates folder (relative to content root).
    /// </summary>
    [Required(ErrorMessage = "TemplatesPath is required")]
    public string TemplatesPath { get; set; } = "EmailTemplates";

    /// <summary>
    /// Whether to use pickup directory instead of SMTP (for development/testing).
    /// When true, emails are saved as .eml files instead of being sent.
    /// </summary>
    public bool UsePickupDirectory { get; set; }

    /// <summary>
    /// Path to pickup directory (used when UsePickupDirectory is true).
    /// </summary>
    public string? PickupDirectoryPath { get; set; }

    /// <summary>
    /// Custom validation for pickup directory configuration.
    /// </summary>
    public bool Validate()
    {
        if (UsePickupDirectory && string.IsNullOrWhiteSpace(PickupDirectoryPath))
        {
            return false;
        }

        if (!UsePickupDirectory && (string.IsNullOrWhiteSpace(SmtpHost) || SmtpPort <= 0))
        {
            return false;
        }

        return true;
    }
}
