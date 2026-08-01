#nullable enable

namespace TicketingSystem.Infrastructure.Email
{
    /// <summary>
    /// Abstraction for sending outbound emails.
    /// </summary>
    public interface IMailSender
    {
        /// <summary>
        /// Sends a simple text (or HTML) email to one or more recipients.
        /// </summary>
        /// <param name="to">Comma- or semicolon-separated list of addresses.</param>
        /// <param name="subject">Email subject line.</param>
        /// <param name="body">
        /// Body text (can be plain-text or HTML, depending on implementation).
        /// </param>
        /// <param name="isHtml">Whether the body should be treated as HTML.</param>
        /// <returns>Task that completes when the email is sent or written to disk.</returns>
        Task SendAsync(string to, string subject, string body, bool isHtml = false);
    }
}