#nullable enable
using MailKit.Security;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

using System.Net;

using TicketingSystem.Api.Common;
using TicketingSystem.Api.Utils;

namespace TicketingSystem.Infrastructure.Email
{
    /// <summary>
    /// Default implementation of IMailSender using MailKit.
    /// Supports SMTP or pickup-directory mode depending on EmailOptions.
    /// </summary>
    public sealed class MailKitSender : IMailSender
    {
        private readonly EmailOptions _options;
        private readonly ILogger<MailKitSender> _logger;

        public MailKitSender(IOptions<EmailOptions> options, ILogger<MailKitSender> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Sends an email according to configuration (SMTP or pickup directory).
        /// </summary>
        public async Task SendAsync(string to, string subject, string body, bool isHtml = false)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Email sending disabled. Skipping send for {To}.", to);
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));

            // support comma or semicolon separated addresses
            foreach (var address in to.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                message.To.Add(MailboxAddress.Parse(address.Trim()));

            message.Subject = subject;

            var bodyBuilder = new BodyBuilder();
            if (isHtml)
                bodyBuilder.HtmlBody = body;
            else
                bodyBuilder.TextBody = body;

            message.Body = bodyBuilder.ToMessageBody();

            if (_options.UsePickupDirectory)
            {
                await WriteToPickupDirectoryAsync(message);
                return;
            }

            await SendViaSmtpAsync(message);
        }

        private async Task WriteToPickupDirectoryAsync(MimeMessage message)
        {
            var directory = _options.PickupDirectoryLocation ?? ".maildrop";
            Directory.CreateDirectory(directory);

            var fileName = Path.Combine(directory, $"{Guid.NewGuid():N}.eml");
            await using var stream = File.Create(fileName);
            await message.WriteToAsync(stream);

            _logger.LogInformation("Email written to pickup directory: {File}", fileName);
        }

        private async Task SendViaSmtpAsync(MimeMessage message)
        {
            using var client = new SmtpClient();

            try
            {
                // Choose TLS mode based on port/config
                SecureSocketOptions socketOptions;
                if (_options.UseSsl)
                {
                    socketOptions = _options.Port == 465
                        ? SecureSocketOptions.SslOnConnect   // implicit SSL for 465
                        : SecureSocketOptions.StartTls;       // STARTTLS for 587/25
                }
                else
                {
                    socketOptions = SecureSocketOptions.None;  // plain (rare)
                }

                _logger.LogInformation("Connecting to SMTP {Host}:{Port} with {Mode} ...",
                    _options.Host, _options.Port, socketOptions);

                await client.ConnectAsync(_options.Host, _options.Port, socketOptions);

                if (!string.IsNullOrEmpty(_options.Username))
                    await client.AuthenticateAsync(_options.Username, _options.Password);

                await client.SendAsync(message);
                _logger.LogInformation("Email sent successfully to {To}", message.To);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {To}", message.To);
                throw new AppException(ErrorCodes.EmailSendFailed,
                    $"Error sending email to {message.To}",
                    HttpStatusCode.ServiceUnavailable);
            }
            finally
            {
                if (client.IsConnected)
                    await client.DisconnectAsync(true);
            }
        }
    }
}