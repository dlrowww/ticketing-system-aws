using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using TicketingSystem.Api.Data;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.Infrastructure.Email;
using TicketingSystem.Api.Services.Localization;

namespace TicketingSystem.Api.Services.Email;

/// <summary>
/// Implementation of IEmailService using MailKit for SMTP email sending.
/// Supports both SMTP server and pickup directory (for development).
/// </summary>
public class EmailService : IEmailService
{
    private readonly AppDbContext _db;
    private readonly ILocalizationService _localization;
    private readonly EmailOptions _options;
    private readonly ILogger<EmailService> _logger;
    private readonly IWebHostEnvironment _environment;

    public EmailService(
        AppDbContext db,
        ILocalizationService localization,
        IOptions<EmailOptions> options,
        ILogger<EmailService> logger,
        IWebHostEnvironment environment)
    {
        _db = db;
        _localization = localization;
        _options = options.Value;
        _logger = logger;
        _environment = environment;
    }

    public async Task SendTicketAssignedAsync(int ticketId, int assigneeId, CancellationToken ct = default)
    {
        try
        {
            var ticket = await GetTicketWithDetailsAsync(ticketId, ct);
            if (ticket == null) return;

            var assignee = await _db.Users.FindAsync(new object[] { assigneeId }, ct);
            if (assignee == null || string.IsNullOrWhiteSpace(assignee.Email)) return;

            var templatePath = Path.Combine(_environment.ContentRootPath, _options.TemplatesPath, "TicketAssigned.html");
            var htmlBody = await RenderTemplateAsync(templatePath, new Dictionary<string, string>
            {
                ["TicketId"] = ticket.TicketId.ToString(),
                ["Title"] = ticket.Title,
                ["Category"] = $"{ticket.Category.NamePl} / {ticket.Category.NameEn}",
                ["Priority"] = _localization.GetBilingualEnum("TicketPriority", ticket.Priority.ToString()),
                ["Status"] = _localization.GetBilingualEnum("TicketStatus", ticket.Status.ToString()),
                ["TicketUrl"] = $"{_options.BaseUrl}/app/tickets/{ticket.TicketId}",
                ["AssigneeName"] = assignee.Name
            });

            await SendEmailAsync(
                new[] { assignee.Email },
                _localization.GetEmailLabel("TicketAssignedSubject", "pl") + " / " + _localization.GetEmailLabel("TicketAssignedSubject", "en"),
                htmlBody,
                ct);

            _logger.LogInformation("Sent TicketAssigned email for ticket {TicketId} to {Email}", ticketId, assignee.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send TicketAssigned email for ticket {TicketId}", ticketId);
        }
    }

    public async Task SendTicketReassignedAsync(int ticketId, int? oldAssigneeId, int newAssigneeId, int reassignedBy, CancellationToken ct = default)
    {
        try
        {
            var ticket = await GetTicketWithDetailsAsync(ticketId, ct);
            if (ticket == null) return;

            var recipients = new HashSet<string>();
            
            // New assignee
            var newAssignee = await _db.Users.FindAsync(new object[] { newAssigneeId }, ct);
            if (newAssignee != null && !string.IsNullOrWhiteSpace(newAssignee.Email))
            {
                recipients.Add(newAssignee.Email);
            }

            // Old assignee (if existed)
            if (oldAssigneeId.HasValue)
            {
                var oldAssignee = await _db.Users.FindAsync(new object[] { oldAssigneeId.Value }, ct);
                if (oldAssignee != null && !string.IsNullOrWhiteSpace(oldAssignee.Email))
                {
                    recipients.Add(oldAssignee.Email);
                }
            }

            if (!recipients.Any()) return;

            var templatePath = Path.Combine(_environment.ContentRootPath, _options.TemplatesPath, "TicketReassigned.html");
            var htmlBody = await RenderTemplateAsync(templatePath, new Dictionary<string, string>
            {
                ["TicketId"] = ticket.TicketId.ToString(),
                ["Title"] = ticket.Title,
                ["Category"] = $"{ticket.Category.NamePl} / {ticket.Category.NameEn}",
                ["Priority"] = _localization.GetBilingualEnum("TicketPriority", ticket.Priority.ToString()),
                ["Status"] = _localization.GetBilingualEnum("TicketStatus", ticket.Status.ToString()),
                ["TicketUrl"] = $"{_options.BaseUrl}/app/tickets/{ticket.TicketId}",
                ["NewAssigneeName"] = newAssignee?.Name ?? "N/A",
                ["OldAssigneeName"] = oldAssigneeId.HasValue ? (await _db.Users.FindAsync(new object[] { oldAssigneeId.Value }, ct))?.Name ?? "N/A" : "N/A"
            });

            await SendEmailAsync(
                recipients,
                _localization.GetEmailLabel("TicketReassignedSubject", "pl") + " / " + _localization.GetEmailLabel("TicketReassignedSubject", "en"),
                htmlBody,
                ct);

            _logger.LogInformation("Sent TicketReassigned email for ticket {TicketId} to {Count} recipients", ticketId, recipients.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send TicketReassigned email for ticket {TicketId}", ticketId);
        }
    }

    public async Task SendTicketStatusChangedAsync(int ticketId, byte oldStatus, byte newStatus, int changedBy, CancellationToken ct = default)
    {
        try
        {
            var ticket = await GetTicketWithDetailsAsync(ticketId, ct);
            if (ticket == null) return;

            var recipients = await GetCreatorAndAssigneeEmailsAsync(ticket, ct);
            if (!recipients.Any()) return;

            var templatePath = Path.Combine(_environment.ContentRootPath, _options.TemplatesPath, "TicketStatusChanged.html");
            var htmlBody = await RenderTemplateAsync(templatePath, new Dictionary<string, string>
            {
                ["TicketId"] = ticket.TicketId.ToString(),
                ["Title"] = ticket.Title,
                ["Category"] = $"{ticket.Category.NamePl} / {ticket.Category.NameEn}",
                ["Priority"] = _localization.GetBilingualEnum("TicketPriority", ticket.Priority.ToString()),
                ["Status"] = _localization.GetBilingualEnum("TicketStatus", ticket.Status.ToString()),
                ["OldStatus"] = _localization.GetBilingualEnum("TicketStatus", ((TicketStatus)oldStatus).ToString()),
                ["NewStatus"] = _localization.GetBilingualEnum("TicketStatus", ((TicketStatus)newStatus).ToString()),
                ["TicketUrl"] = $"{_options.BaseUrl}/app/tickets/{ticket.TicketId}"
            });

            await SendEmailAsync(
                recipients,
                _localization.GetEmailLabel("TicketStatusChangedSubject", "pl") + " / " + _localization.GetEmailLabel("TicketStatusChangedSubject", "en"),
                htmlBody,
                ct);

            _logger.LogInformation("Sent TicketStatusChanged email for ticket {TicketId} to {Count} recipients", ticketId, recipients.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send TicketStatusChanged email for ticket {TicketId}", ticketId);
        }
    }

    public async Task SendCommentAddedAsync(int ticketId, int commentId, int commenterId, CancellationToken ct = default)
    {
        try
        {
            var ticket = await GetTicketWithDetailsAsync(ticketId, ct);
            if (ticket == null) return;

            var comment = await _db.TicketComments
                .FirstOrDefaultAsync(c => c.CommentId == commentId, ct);
            if (comment == null) return;

            // Do not send emails for internal comments
            if (comment.IsInternal)
            {
                _logger.LogDebug("Skipping email for internal comment {CommentId} on ticket {TicketId}", commentId, ticketId);
                return;
            }

            var recipients = await GetCreatorAndAssigneeEmailsAsync(ticket, ct);
            
            // Exclude commenter
            var commenter = await _db.Users.FindAsync(new object[] { commenterId }, ct);
            if (commenter != null && !string.IsNullOrWhiteSpace(commenter.Email))
            {
                recipients.Remove(commenter.Email);
            }

            if (!recipients.Any()) return;

            // Get commenter name
            var commenterName = commenter?.Name ?? "Unknown";

            var templatePath = Path.Combine(_environment.ContentRootPath, _options.TemplatesPath, "CommentAdded.html");
            var htmlBody = await RenderTemplateAsync(templatePath, new Dictionary<string, string>
            {
                ["TicketId"] = ticket.TicketId.ToString(),
                ["Title"] = ticket.Title,
                ["Category"] = $"{ticket.Category.NamePl} / {ticket.Category.NameEn}",
                ["Priority"] = _localization.GetBilingualEnum("TicketPriority", ticket.Priority.ToString()),
                ["Status"] = _localization.GetBilingualEnum("TicketStatus", ticket.Status.ToString()),
                ["CommenterName"] = commenterName,
                ["CommentContent"] = comment.Content.Length > 200 ? comment.Content.Substring(0, 200) + "..." : comment.Content,
                ["TicketUrl"] = $"{_options.BaseUrl}/app/tickets/{ticket.TicketId}"
            });

            await SendEmailAsync(
                recipients,
                _localization.GetEmailLabel("CommentAddedSubject", "pl") + " / " + _localization.GetEmailLabel("CommentAddedSubject", "en"),
                htmlBody,
                ct);

            _logger.LogInformation("Sent CommentAdded email for ticket {TicketId} to {Count} recipients", ticketId, recipients.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send CommentAdded email for ticket {TicketId}", ticketId);
        }
    }

    public async Task SendPriorityEscalatedAsync(int ticketId, byte oldPriority, byte newPriority, int changedBy, CancellationToken ct = default)
    {
        try
        {
            var ticket = await GetTicketWithDetailsAsync(ticketId, ct);
            if (ticket == null) return;

            var recipients = await GetCreatorAndAssigneeEmailsAsync(ticket, ct);
            if (!recipients.Any()) return;

            var templatePath = Path.Combine(_environment.ContentRootPath, _options.TemplatesPath, "PriorityEscalated.html");
            var htmlBody = await RenderTemplateAsync(templatePath, new Dictionary<string, string>
            {
                ["TicketId"] = ticket.TicketId.ToString(),
                ["Title"] = ticket.Title,
                ["Category"] = $"{ticket.Category.NamePl} / {ticket.Category.NameEn}",
                ["Priority"] = _localization.GetBilingualEnum("TicketPriority", ticket.Priority.ToString()),
                ["Status"] = _localization.GetBilingualEnum("TicketStatus", ticket.Status.ToString()),
                ["OldPriority"] = _localization.GetBilingualEnum("TicketPriority", ((TicketPriority)oldPriority).ToString()),
                ["NewPriority"] = _localization.GetBilingualEnum("TicketPriority", ((TicketPriority)newPriority).ToString()),
                ["TicketUrl"] = $"{_options.BaseUrl}/app/tickets/{ticket.TicketId}"
            });

            await SendEmailAsync(
                recipients,
                _localization.GetEmailLabel("PriorityEscalatedSubject", "pl") + " / " + _localization.GetEmailLabel("PriorityEscalatedSubject", "en"),
                htmlBody,
                ct);

            _logger.LogInformation("Sent PriorityEscalated email for ticket {TicketId} to {Count} recipients", ticketId, recipients.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send PriorityEscalated email for ticket {TicketId}", ticketId);
        }
    }

    public async Task SendTicketResolvedAsync(int ticketId, int resolvedBy, CancellationToken ct = default)
    {
        try
        {
            var ticket = await GetTicketWithDetailsAsync(ticketId, ct);
            if (ticket == null) return;

            var creator = await _db.Users.FindAsync(new object[] { ticket.CreatedById }, ct);
            if (creator == null || string.IsNullOrWhiteSpace(creator.Email)) return;

            var resolver = await _db.Users.FindAsync(new object[] { resolvedBy }, ct);
            var resolutionTime = ticket.UpdatedAt.HasValue && ticket.CreatedAt < ticket.UpdatedAt.Value
                ? (ticket.UpdatedAt.Value - ticket.CreatedAt).TotalHours
                : 0;

            var templatePath = Path.Combine(_environment.ContentRootPath, _options.TemplatesPath, "TicketResolved.html");
            var htmlBody = await RenderTemplateAsync(templatePath, new Dictionary<string, string>
            {
                ["TicketId"] = ticket.TicketId.ToString(),
                ["Title"] = ticket.Title,
                ["Category"] = $"{ticket.Category.NamePl} / {ticket.Category.NameEn}",
                ["Priority"] = _localization.GetBilingualEnum("TicketPriority", ticket.Priority.ToString()),
                ["Status"] = _localization.GetBilingualEnum("TicketStatus", ticket.Status.ToString()),
                ["ResolvedByName"] = resolver?.Name ?? "Unknown",
                ["ResolutionTime"] = $"{resolutionTime:F1} h",
                ["TicketUrl"] = $"{_options.BaseUrl}/app/tickets/{ticket.TicketId}"
            });

            await SendEmailAsync(
                new[] { creator.Email },
                _localization.GetEmailLabel("TicketResolvedSubject", "pl") + " / " + _localization.GetEmailLabel("TicketResolvedSubject", "en"),
                htmlBody,
                ct);

            _logger.LogInformation("Sent TicketResolved email for ticket {TicketId} to {Email}", ticketId, creator.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send TicketResolved email for ticket {TicketId}", ticketId);
        }
    }

    private async Task<Models.Ticket?> GetTicketWithDetailsAsync(int ticketId, CancellationToken ct)
    {
        return await _db.Tickets
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.TicketId == ticketId, ct);
    }

    private async Task<HashSet<string>> GetCreatorAndAssigneeEmailsAsync(Models.Ticket ticket, CancellationToken ct)
    {
        var recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Creator
        var creator = await _db.Users.FindAsync(new object[] { ticket.CreatedById }, ct);
        if (creator != null && !string.IsNullOrWhiteSpace(creator.Email))
        {
            recipients.Add(creator.Email);
        }

        // Assignee
        if (ticket.AssignedToId.HasValue)
        {
            var assignee = await _db.Users.FindAsync(new object[] { ticket.AssignedToId.Value }, ct);
            if (assignee != null && !string.IsNullOrWhiteSpace(assignee.Email))
            {
                recipients.Add(assignee.Email);
            }
        }

        return recipients;
    }

    private async Task<string> RenderTemplateAsync(string templatePath, Dictionary<string, string> placeholders)
    {
        if (!File.Exists(templatePath))
        {
            _logger.LogError("Email template not found: {TemplatePath}", templatePath);
            return "<html><body>Error: Template not found</body></html>";
        }

        var template = await File.ReadAllTextAsync(templatePath);
        
        foreach (var (key, value) in placeholders)
        {
            template = template.Replace($"{{{{{key}}}}}", value);
        }

        return template;
    }

    private async Task SendEmailAsync(IEnumerable<string> recipients, string subject, string htmlBody, CancellationToken ct)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
            
            foreach (var recipient in recipients)
            {
                message.To.Add(MailboxAddress.Parse(recipient));
            }

            message.Subject = subject;

            // Create multipart/related message to embed logo
            var builder = new BodyBuilder();
            builder.HtmlBody = htmlBody;

            // Attach IronPack logo as embedded image (accessible via cid:ironpack-logo)
            var logoPath = Path.Combine(_environment.ContentRootPath, "wwwroot", "ticketing-system-logo.svg");
            if (File.Exists(logoPath))
            {
                var logo = builder.LinkedResources.Add(logoPath);
                logo.ContentId = "ironpack-logo";
                logo.ContentDisposition = new MimeKit.ContentDisposition(MimeKit.ContentDisposition.Inline);
            }
            else
            {
                _logger.LogWarning("IronPack logo not found at {LogoPath}. Emails will be sent without logo.", logoPath);
            }

            message.Body = builder.ToMessageBody();

            if (_options.UsePickupDirectory)
            {
                await SaveToPickupDirectoryAsync(message);
            }
            else
            {
                await SendViaSmtpAsync(message, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipients}", string.Join(", ", recipients));
            throw;
        }
    }

    private async Task SaveToPickupDirectoryAsync(MimeMessage message)
    {
        if (string.IsNullOrWhiteSpace(_options.PickupDirectoryPath))
        {
            _logger.LogError("PickupDirectoryPath is not configured");
            return;
        }

        Directory.CreateDirectory(_options.PickupDirectoryPath);
        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.eml";
        var filePath = Path.Combine(_options.PickupDirectoryPath, fileName);
        
        await message.WriteToAsync(filePath);
        _logger.LogInformation("Email saved to pickup directory: {FilePath}", filePath);
    }

    private async Task SendViaSmtpAsync(MimeMessage message, CancellationToken ct)
    {
        using var client = new SmtpClient();
        await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, _options.UseSsl, ct);
        
        if (!string.IsNullOrWhiteSpace(_options.Username) && !string.IsNullOrWhiteSpace(_options.Password))
        {
            await client.AuthenticateAsync(_options.Username, _options.Password, ct);
        }

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
        
        _logger.LogInformation("Email sent via SMTP to {Recipients}", string.Join(", ", message.To));
    }
}
