using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System.Net;
using System.Text;

using TicketingSystem.Api.Common;
using TicketingSystem.Api.Data;
using TicketingSystem.Api.DTOs.Comments;
using TicketingSystem.Api.DTOs.History;
using TicketingSystem.Api.DTOs.Tickets;
using TicketingSystem.Api.DTOs.Users;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.Services;
using TicketingSystem.Api.Utils;

namespace TicketingSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _tickets;
    private readonly AppDbContext _db;
    private readonly ITicketAttachmentService _attachments;
    private readonly ICommentService _comments;
    private readonly ITicketHistoryService _history;
    private readonly ICurrentUserService _currentUser;

    public TicketsController(
        ITicketService tickets, 
        AppDbContext db, 
        ITicketAttachmentService attachments, 
        ICommentService comments,
        ITicketHistoryService history,
        ICurrentUserService currentUser)
    {
        _tickets = tickets;
        _db = db;
        _attachments = attachments;
        _comments = comments;
        _history = history;
        _currentUser = currentUser;
    }

    /// <summary>Create a ticket. Send as multipart/form-data if Files are included.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(60L * 1024 * 1024)] // 60 MB request cap (tweak if needed)
    public async Task<IActionResult> Create([FromForm] CreateTicketRequest req, CancellationToken ct)
    {
        var created = await _tickets.CreateAsync(req, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.TicketId }, created);
    }

    /// <summary>Update ticket (title, description, category, status, priority, assignment)</summary>
    [HttpPatch("{id:int}")]
    public async Task<ActionResult<TicketDetailsDto>> UpdateTicket([FromRoute] int id, [FromBody] UpdateTicketRequest req, CancellationToken ct)
    {
        var dto = await _tickets.UpdateAsync(id, req, ct);
        return Ok(dto);
    }

    /// <summary>Fetch a single ticket.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var dto = await _tickets.GetByIdAsync(id, ct);
        if (dto is null)
        {
            throw new AppException(ErrorCodes.TicketNotFound,
                "Ticket {id} is not found",
                HttpStatusCode.NotFound);
        }
        return Ok(dto);
    }

    [HttpGet]
    public async Task<ActionResult<Paged<TicketListItemDto>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] byte? status = null,
        [FromQuery] byte? category = null,
        [FromQuery] byte? priority = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        [FromQuery] int? createdByUserId = null,
        [FromQuery] int? assignedToUserId = null,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortDir = "desc",
        CancellationToken ct = default)
    {
        var assignedToIsNull = Request.Query.ContainsKey("assignedToUserId") &&
                               string.IsNullOrWhiteSpace(Request.Query["assignedToUserId"].ToString());

        var asc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        var result = await _tickets.GetListAsync(new TicketListQuery
        {
            Page = new PageRequest { Page = page, Size = pageSize, Sort = $"{(asc ? "" : "-")}{(sortBy ?? "createdAt")}" },
            Search   = search,
            Status   = status,
            CategoryId = category,
            Priority = priority,
            DateFrom = dateFrom,
            DateTo   = dateTo,
            CreatedByUserId = createdByUserId,
            AssignedToUserId = assignedToUserId,
            AssignedToIsNull = assignedToIsNull,
            SortBy   = sortBy ?? "createdAt",
            SortDir  = sortDir ?? "desc"
        }, ct);

        return Ok(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? search = null,
        [FromQuery] byte? status = null,
        [FromQuery] byte? category = null,
        [FromQuery] byte? priority = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        [FromQuery] int? createdByUserId = null,
        [FromQuery] int? assignedToUserId = null,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortDir = "desc",
        CancellationToken ct = default)
    {
        var assignedToIsNull = Request.Query.ContainsKey("assignedToUserId") &&
                               string.IsNullOrWhiteSpace(Request.Query["assignedToUserId"].ToString());

        var rows = await _tickets.ExportAsync(new TicketExportQuery
        {
            Search   = search,
            Status   = status,
            CategoryId = category,
            Priority = priority,
            DateFrom = dateFrom,
            DateTo   = dateTo,
            CreatedByUserId = createdByUserId,
            AssignedToUserId = assignedToUserId,
            AssignedToIsNull = assignedToIsNull,
            SortBy   = sortBy ?? "createdAt",
            SortDir  = sortDir ?? "desc"
        }, ct);

        var csv = ToCsv(rows);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", "tickets.csv");
    }

    /// <summary>
    /// Get users who can be assigned to this ticket.
    /// Filters by role (Support/TeamLeader/Admin) and category.
    /// </summary>
    [HttpGet("{id:int}/assignable-users")]
    [ProducesResponseType(typeof(IReadOnlyList<AssignableUserDto>), 200)]
    public async Task<ActionResult<IReadOnlyList<AssignableUserDto>>> GetAssignableUsers(
        [FromRoute] int id,
        CancellationToken ct)
    {
        var users = await _tickets.GetAssignableUsersAsync(id, ct);
        return Ok(users);
    }

    [HttpGet("{id:int}/files")]
    public async Task<ActionResult<IEnumerable<TicketFileDto>>> ListFiles([FromRoute] int id, CancellationToken ct)
    {
        var files = await _attachments.ListAsync(id, ct);
        return Ok(files);
    }

    /// <summary>Add files to an existing ticket. Send as multipart/form-data.</summary>
    [HttpPost("{id:int}/files")]
    [RequestSizeLimit(60L * 1024 * 1024)] // 60 MB request cap (tweak if needed)
    public async Task<ActionResult<IReadOnlyList<TicketFileDto>>> AddFiles([FromRoute] int id, [FromForm] IFormFile[] files, CancellationToken ct)
    {
        // Verify ticket exists
        var ticket = await _db.Tickets.FindAsync([id], ct);
        if (ticket is null)
        {
            throw new AppException(ErrorCodes.TicketNotFound,
                $"Ticket {id} not found",
                HttpStatusCode.NotFound);
        }

        // Get current user ID (will throw if not authenticated)
        var uploaderUserId = _currentUser.GetUserId();

        // Add files
        var uploadedFiles = await _attachments.AddAsync(id, files, uploaderUserId, ct);
        return Ok(uploadedFiles);
    }

    [HttpGet("{id:int}/files/{fileId:int}")]
    public async Task<IActionResult> DownloadFile([FromRoute] int id, [FromRoute] int fileId, [FromQuery] bool inline = false, CancellationToken ct = default)
    {
        var df = await _attachments.OpenForDownloadAsync(id, fileId, ct);
        
        // If inline=true, display in browser; otherwise force download
        if (inline)
        {
            // Set Content-Disposition to inline for browser preview with proper UTF-8 encoding
            var contentDisposition = new ContentDispositionHeaderValue("inline");
            contentDisposition.SetHttpFileName(df.OriginalName);
            Response.Headers.ContentDisposition = contentDisposition.ToString();
            return File(df.Content, df.ContentType, enableRangeProcessing: true);
        }
        else
        {
            // Force download with attachment disposition
            return File(df.Content, df.ContentType, df.OriginalName, enableRangeProcessing: true);
        }
    }

    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<CommentDto>> AddComment([FromRoute] int id, [FromBody] AddCommentRequest req, CancellationToken ct)
    {
        var dto = await _comments.AddAsync(id, req, ct);
        return CreatedAtAction(nameof(GetComments), new { id }, dto); // 201 with Location to list
    }

    [HttpGet("{id:int}/comments")]
    public async Task<ActionResult<IReadOnlyList<CommentDto>>> GetComments([FromRoute] int id, CancellationToken ct)
    {
        var list = await _comments.ListAsync(id, ct);
        return Ok(list);
    }

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<IReadOnlyList<TicketHistoryDto>>> GetHistory([FromRoute] int id, CancellationToken ct)
    {
        var history = await _history.GetHistoryAsync(id, ct);
        return Ok(history);
    }

    /// <summary>
    /// Get list of allowed status transitions for a ticket.
    /// Always includes the current status itself.
    /// </summary>
    [HttpGet("{id:int}/allowed-statuses")]
    public async Task<ActionResult<AllowedStatusesDto>> GetAllowedStatuses([FromRoute] int id, CancellationToken ct)
    {
        var ticket = await _tickets.GetByIdAsync(id, ct);
        if (ticket == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://httpstatuses.com/404",
                Title = "Ticket not found",
                Status = 404,
                Detail = $"Ticket with ID {id} was not found.",
                Instance = HttpContext.Request.Path
            });
        }

        var currentStatus = (TicketStatus)ticket.Status;
        var allowedStatuses = TicketRules.GetAllowedStatuses(currentStatus);
        var dto = new AllowedStatusesDto
        {
            AllowedStatuses = allowedStatuses.Select(s => (byte)s).ToList()
        };

        return Ok(dto);
    }

    // --- mapping & helpers ---
    private static string ToCsv(IEnumerable<TicketListItemDto> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ticketId,title,category,priority,status,createdAt,updatedAt,createdByName,assignedToName");
        foreach (var t in items)
        {
            sb.AppendLine(string.Join(',', new[]
            {
                t.TicketId.ToString(),
                CsvEscape(t.Title),
                t.CategoryId.ToString(),
                t.Priority.ToString(),
                t.Status.ToString(),
                t.CreatedAt.ToString("O"),
                t.UpdatedAt?.ToString("O") ?? "",
                CsvEscape(t.CreatedByName),
                CsvEscape(t.AssignedToName)
            }));
        }
        return sb.ToString();
    }

    private static string CsvEscape(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var needQuotes = s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
        var escaped = s.Replace("\"", "\"\"");
        return needQuotes ? $"\"{escaped}\"" : escaped;
    }
}