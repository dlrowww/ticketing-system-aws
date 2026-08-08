using Microsoft.EntityFrameworkCore;
using System.Net;

using TicketingSystem.Api.Common;
using TicketingSystem.Api.Data;
using TicketingSystem.Api.DTOs.Tickets;
using TicketingSystem.Api.DTOs.Users;
using TicketingSystem.Api.Enums.History;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.Models;
using TicketingSystem.Api.Services.Email;
using TicketingSystem.Api.Services.Tickets;
using TicketingSystem.Api.Services.Tickets.Policies;
using TicketingSystem.Api.Utils;
using TicketingSystem.Api.Validators;

namespace TicketingSystem.Api.Services
{
    public sealed class TicketService : ITicketService
    {
        private readonly AppDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IAssignmentService _assignment;
        private readonly ITicketAttachmentService _attachments;
        private readonly ITicketValidator _validator;
        private readonly ITicketUpdateValidator _updateValidator;
        private readonly ITicketAssignmentValidator _assignmentValidator;
        private readonly ITicketHistoryService _history;
        private readonly IEmailService _email;
        private readonly ILogger<TicketService> _logger;

        public TicketService(
            AppDbContext db,
            ICurrentUserService currentUser,
            IAssignmentService assignment,
            ITicketAttachmentService attachments,
            ITicketValidator validator,
            ITicketUpdateValidator updateValidator,
            ITicketAssignmentValidator assignmentValidator,
            ITicketHistoryService history,
            IEmailService email,
            ILogger<TicketService> logger)
        {
            _db = db;
            _currentUser = currentUser;
            _assignment = assignment;
            _attachments = attachments;
            _validator = validator;
            _updateValidator = updateValidator;
            _assignmentValidator = assignmentValidator;
            _history = history;
            _email = email;
            _logger = logger;
        }

        public async Task<CreateTicketResponse> CreateAsync(CreateTicketRequest request, CancellationToken ct)
        {
            var ticketRequest = _validator.ValidateAndNormalize(request);

            var userId = _currentUser.GetUserId();

            // Resolve assignee first (can be null)
            var assignedTo = await _assignment.ResolveAssigneeAsync(ticketRequest.CategoryId, ct);

            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var ticket = new Ticket
                {
                    Title = ticketRequest.Title.Trim(),
                    Description = ticketRequest.Description,
                    CategoryId = ticketRequest.CategoryId,
                    Priority = ticketRequest.Priority,
                    Status = TicketStatus.New,
                    CreatedById = userId,
                    AssignedToId = assignedTo
                };

                _db.Tickets.Add(ticket);
                await _db.SaveChangesAsync(ct);                  // TicketId generated here

                // Log ticket creation in history
                await _history.LogChangeAsync(
                    ticket.TicketId,
                    HistoryChangeType.TicketCreated,
                    oldValue: null,
                    newValue: $"Status: {ticket.Status} | CategoryId: {ticket.CategoryId} | Priority: {(TicketPriority)ticket.Priority}",
                    changedBy: userId,
                    ct
                );

                // Log initial assignment if auto-assigned
                if (assignedTo.HasValue)
                {
                    await _history.LogChangeAsync(
                        ticket.TicketId,
                        HistoryChangeType.AssignmentChanged,
                        oldValue: null,
                        newValue: assignedTo.Value.ToString(),
                        changedBy: userId,
                        ct
                    );
                }

                // Attachments
                if (request.Files is { Count: > 0 })
                {
                    await _attachments.AddAsync(ticket.TicketId, request.Files.ToArray(), userId, ct);
                }

                await tx.CommitAsync(ct);

                // EmailService is scoped and shares this request's DbContext. Await the
                // notification so it cannot query that context concurrently or outlive
                // the request scope.
                if (assignedTo.HasValue)
                {
                    try
                    {
                        await _email.SendTicketAssignedAsync(ticket.TicketId, assignedTo.Value, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send TicketAssigned email for ticket {TicketId}", ticket.TicketId);
                    }
                }

                var createdAtUtc = ticket.CreatedAt.Kind == DateTimeKind.Utc
                    ? ticket.CreatedAt
                    : DateTime.SpecifyKind(ticket.CreatedAt, DateTimeKind.Utc);

                return new CreateTicketResponse(
                    TicketId: ticket.TicketId,
                    Status: ticket.Status,
                    AssignedToUserId: ticket.AssignedToId,
                    CreatedAt: createdAtUtc
                );
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw; // Global ProblemDetails handler will convert to RFC7807 + code
            }
        }

        public async Task<TicketDetailsDto> UpdateAsync(int id, UpdateTicketRequest req, CancellationToken ct)
        {
            var normalized = _updateValidator.ValidateAndNormalize(req);

            var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.TicketId == id, ct);
            if (ticket is null)
                throw new AppException(ErrorCodes.TicketNotFound, $"Ticket {id} not found", HttpStatusCode.NotFound);

            // Get current user and compute edit capabilities
            var userId = _currentUser.GetUserId();
            var currentUser = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId, ct);

            if (currentUser is null)
                throw new AppException(ErrorCodes.UserNotFound, $"Current user {userId} not found", HttpStatusCode.InternalServerError);

            var capabilities = TicketEditPolicy.ComputeCapabilities(ticket, currentUser);

            // Permission enforcement: check if user has capability to edit at all
            if (!capabilities.CanEdit)
                throw new AppException(
                    ErrorCodes.ForbiddenOperation, 
                    "You do not have permission to edit this ticket", 
                    HttpStatusCode.Forbidden
                );

            // Field-level permission checks
            if (normalized.Title is not null && !capabilities.CanEditTitle)
                throw new AppException(
                    ErrorCodes.ForbiddenOperation, 
                    "You do not have permission to edit the title", 
                    HttpStatusCode.Forbidden
                );

            if (normalized.Description is not null && !capabilities.CanEditDescription)
                throw new AppException(
                    ErrorCodes.ForbiddenOperation, 
                    "You do not have permission to edit the description", 
                    HttpStatusCode.Forbidden
                );

            if (req.CategoryId.HasValue && !capabilities.CanEditCategory)
                throw new AppException(
                    ErrorCodes.ForbiddenOperation, 
                    "You do not have permission to edit the category", 
                    HttpStatusCode.Forbidden
                );

            if (req.Priority.HasValue && !capabilities.CanEditPriority)
                throw new AppException(
                    ErrorCodes.ForbiddenOperation, 
                    "You do not have permission to edit the priority", 
                    HttpStatusCode.Forbidden
                );

            if (req.Status.HasValue && !capabilities.CanEditStatus)
                throw new AppException(
                    ErrorCodes.ForbiddenOperation, 
                    "You do not have permission to edit the status", 
                    HttpStatusCode.Forbidden
                );

            if ((req.AssignedToUserId.HasValue || req.ClearAssignment == true) && !capabilities.CanEditAssignment)
                throw new AppException(
                    ErrorCodes.ForbiddenOperation, 
                    "You do not have permission to edit the assignment", 
                    HttpStatusCode.Forbidden
                );

            // Capture old values for history logging
            var oldStatus = ticket.Status;
            var oldPriority = ticket.Priority;
            var oldCategoryId = ticket.CategoryId;
            var oldAssignedToId = ticket.AssignedToId;
            var oldTitle = ticket.Title;
            var oldDescription = ticket.Description;

            // business validation: status transition
            if (req.Status.HasValue && !TicketRules.IsAllowedTransition(ticket.Status, req.Status.Value))
                throw new AppException(ErrorCodes.TicketStatusTransitionInvalid, "Illegal status change.", HttpStatusCode.Conflict);

            // assignment checks
            if (req.AssignedToUserId.HasValue)
            {
                await _assignmentValidator.ValidateAssignmentAsync(ticket.CategoryId, req.AssignedToUserId.Value, ct);
                ticket.AssignedToId = req.AssignedToUserId.Value;
            }
            else if (req.ClearAssignment == true)
            {
                ticket.AssignedToId = null;
            }

            // text fields
            if (normalized.Title is not null)       ticket.Title = normalized.Title;
            if (normalized.Description is not null) ticket.Description = normalized.Description;

            // category and enums
            if (req.CategoryId.HasValue) ticket.CategoryId = req.CategoryId.Value;
            if (req.Priority.HasValue) ticket.Priority = req.Priority.Value;
            if (req.Status.HasValue)   ticket.Status   = req.Status.Value;

            ticket.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            // Log history for changed fields
            if (req.Status.HasValue && oldStatus != ticket.Status)
            {
                await _history.LogChangeAsync(
                    id,
                    HistoryChangeType.StatusChanged,
                    oldValue: oldStatus.ToString(),
                    newValue: ticket.Status.ToString(),
                    changedBy: userId,
                    ct
                );
            }

            if (req.Priority.HasValue && oldPriority != ticket.Priority)
            {
                await _history.LogChangeAsync(
                    id,
                    HistoryChangeType.PriorityChanged,
                    oldValue: oldPriority.ToString(),
                    newValue: ticket.Priority.ToString(),
                    changedBy: userId,
                    ct
                );
            }

            if (req.CategoryId.HasValue && oldCategoryId != ticket.CategoryId)
            {
                await _history.LogChangeAsync(
                    id,
                    HistoryChangeType.CategoryChanged,
                    oldValue: oldCategoryId.ToString(),
                    newValue: ticket.CategoryId.ToString(),
                    changedBy: userId,
                    ct
                );
            }

            if ((req.AssignedToUserId.HasValue || req.ClearAssignment == true) && oldAssignedToId != ticket.AssignedToId)
            {
                await _history.LogChangeAsync(
                    id,
                    HistoryChangeType.AssignmentChanged,
                    oldValue: oldAssignedToId?.ToString(),
                    newValue: ticket.AssignedToId?.ToString(),
                    changedBy: userId,
                    ct
                );
            }

            // Optional: Log title changes
            if (normalized.Title is not null && oldTitle != ticket.Title)
            {
                await _history.LogChangeAsync(
                    id,
                    HistoryChangeType.TitleChanged,
                    oldValue: oldTitle,
                    newValue: ticket.Title,
                    changedBy: userId,
                    ct
                );
            }

            // Optional: Log description changes (truncate for brevity)
            if (normalized.Description is not null && oldDescription != ticket.Description)
            {
                await _history.LogChangeAsync(
                    id,
                    HistoryChangeType.DescriptionChanged,
                    oldValue: TruncateForHistory(oldDescription),
                    newValue: TruncateForHistory(ticket.Description),
                    changedBy: userId,
                    ct
                );
            }

            // EmailService is scoped and shares this request's DbContext. These calls
            // must be awaited; Task.Run would use the same context concurrently with
            // the response queries below and could continue after scope disposal.
            // 1. Reassignment notification
            if ((req.AssignedToUserId.HasValue || req.ClearAssignment == true) && oldAssignedToId != ticket.AssignedToId)
            {
                if (ticket.AssignedToId.HasValue)
                {
                    try
                    {
                        await _email.SendTicketReassignedAsync(
                            ticket.TicketId,
                            oldAssignedToId,
                            ticket.AssignedToId.Value,
                            userId,
                            ct
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send TicketReassigned email for ticket {TicketId}", ticket.TicketId);
                    }
                }
            }

            // 2. Ticket resolved notification
            if (req.Status.HasValue && ticket.Status == TicketStatus.Resolved && oldStatus != TicketStatus.Resolved)
            {
                try
                {
                    await _email.SendTicketResolvedAsync(ticket.TicketId, userId, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send TicketResolved email for ticket {TicketId}", ticket.TicketId);
                }
            }
            // 3. Status changed notification (non-resolution)
            else if (req.Status.HasValue && oldStatus != ticket.Status)
            {
                try
                {
                    await _email.SendTicketStatusChangedAsync(
                        ticket.TicketId,
                        (byte)oldStatus,
                        (byte)ticket.Status,
                        userId,
                        ct
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send TicketStatusChanged email for ticket {TicketId}", ticket.TicketId);
                }
            }

            // 4. Priority escalation notification (only if escalating to High or Critical)
            if (req.Priority.HasValue && 
                ticket.Priority >= TicketPriority.High && 
                oldPriority < TicketPriority.High)
            {
                try
                {
                    await _email.SendPriorityEscalatedAsync(
                        ticket.TicketId,
                        (byte)oldPriority,
                        (byte)ticket.Priority,
                        userId,
                        ct
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send PriorityEscalated email for ticket {TicketId}", ticket.TicketId);
                }
            }

            // return full details with capabilities (re-fetch ticket to get updated state)
            var updatedTicket = await _db.Tickets
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TicketId == id, ct);

            if (updatedTicket is null)
                throw new AppException(ErrorCodes.TicketNotFound, $"Ticket {id} not found after update", HttpStatusCode.NotFound);

            // Recompute capabilities on updated ticket (currentUser already fetched at start)
            var updatedCapabilities = TicketEditPolicy.ComputeCapabilities(updatedTicket, currentUser);

            var dto = new TicketDetailsDto
            {
                TicketId = updatedTicket.TicketId,
                Title = updatedTicket.Title,
                Description = updatedTicket.Description,
                CategoryId = updatedTicket.CategoryId,
                Priority = (int)updatedTicket.Priority,
                Status = (int)updatedTicket.Status,
                CreatedAt = updatedTicket.CreatedAt,
                UpdatedAt = updatedTicket.UpdatedAt,
                CreatedById = updatedTicket.CreatedById,
                AssignedToId = updatedTicket.AssignedToId,
                CreatedByName = await _db.Users
                    .Where(u => u.UserId == updatedTicket.CreatedById)
                    .Select(u => u.Name)
                    .FirstOrDefaultAsync(ct),
                AssignedToName = updatedTicket.AssignedToId == null ? null :
                    await _db.Users
                        .Where(u => u.UserId == updatedTicket.AssignedToId)
                        .Select(u => u.Name)
                        .FirstOrDefaultAsync(ct),
                Capabilities = updatedCapabilities
            };

            return dto;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            // Delete file blobs
            var fileIdsForTicket = _db.TicketFiles
                .Where(f => f.TicketId == id)
                .Select(f => f.TicketFileId);

            await _db.TicketFileContents
                .Where(c => fileIdsForTicket.Contains(c.TicketFileId))
                .ExecuteDeleteAsync(ct);

            // Delete file metadata
            await _db.TicketFiles
                .Where(f => f.TicketId == id)
                .ExecuteDeleteAsync(ct);

            // Delete comments
            await _db.TicketComments
                .Where(c => c.TicketId == id)
                .ExecuteDeleteAsync(ct);

            // Delete the ticket (if it existed)
            var affected = await _db.Tickets
                .Where(t => t.TicketId == id)
                .ExecuteDeleteAsync(ct);

            await tx.CommitAsync(ct);
            return affected > 0; // false => not found, true -> deleted
        }

        public async Task<TicketDetailsDto?> GetByIdAsync(int id, CancellationToken ct)
        {
            var ticket = await _db.Tickets
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TicketId == id, ct);
            
            if (ticket is null)
                return null;

            // Get current user for capability computation
            var userId = _currentUser.GetUserId();
            var currentUser = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId, ct);

            if (currentUser is null)
                throw new AppException(ErrorCodes.UserNotFound, $"Current user {userId} not found", HttpStatusCode.InternalServerError);

            // Compute edit capabilities
            var capabilities = TicketEditPolicy.ComputeCapabilities(ticket, currentUser);

            // Build DTO with capabilities
            var dto = new TicketDetailsDto
            {
                TicketId = ticket.TicketId,
                Title = ticket.Title,
                Description = ticket.Description,
                CategoryId = ticket.CategoryId,
                Priority = (int)ticket.Priority,
                Status = (int)ticket.Status,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt,
                CreatedById = ticket.CreatedById,
                AssignedToId = ticket.AssignedToId,
                CreatedByName = await _db.Users
                    .Where(u => u.UserId == ticket.CreatedById)
                    .Select(u => u.Name)
                    .FirstOrDefaultAsync(ct),
                AssignedToName = ticket.AssignedToId == null ? null :
                    await _db.Users
                        .Where(u => u.UserId == ticket.AssignedToId)
                        .Select(u => u.Name)
                        .FirstOrDefaultAsync(ct),
                Capabilities = capabilities
            };

            return dto;
        }

        public async Task<Paged<TicketListItemDto>> GetListAsync(TicketListQuery q, CancellationToken ct)
        {
            var qry = ApplyFiltersAndSort(
                _db.Tickets.AsNoTracking(),
                q.Search, q.Status, q.CategoryId, q.Priority,
                q.DateFrom, q.DateTo, q.SortBy, q.SortDir);

            if (q.CreatedByUserId.HasValue)
            {
                qry = qry.Where(t => t.CreatedById == q.CreatedByUserId.Value);
            }

            if (q.AssignedToIsNull)
            {
                qry = qry.Where(t => t.AssignedToId == null);
            }
            else if (q.AssignedToUserId.HasValue)
            {
                qry = qry.Where(t => t.AssignedToId == q.AssignedToUserId.Value);
            }

            // paging
            var asc = string.Equals(q.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
            var sortKey = (q.SortBy ?? "createdAt").Replace("_", "").ToLowerInvariant();

            var req = new PageRequest { Page = q.Page.Page, Size = q.Page.Size, Sort = $"{(asc ? "" : "-")}{sortKey}" };
            var (skip, take) = req.ToSkipTake();

            var total = await qry.CountAsync(ct);

            var items = await qry
                .Skip(skip).Take(take)
                .Select(t => new TicketListItemDto(
                    t.TicketId,
                    t.Title,
                    t.CategoryId,
                    (byte)t.Priority,
                    (byte)t.Status,
                    t.CreatedAt,
                    t.UpdatedAt,
                    _db.Users.Where(u => u.UserId == t.CreatedById).Select(u => u.Name).FirstOrDefault(),
                    t.AssignedToId == null
                        ? null
                        : _db.Users.Where(u => u.UserId == t.AssignedToId).Select(u => u.Name).FirstOrDefault()
                ))
                .ToListAsync(ct);

            return new Paged<TicketListItemDto>(items, total, req);
        }

        public async Task<IReadOnlyList<TicketListItemDto>> ExportAsync(TicketExportQuery q, CancellationToken ct)
        {
            var qry = ApplyFiltersAndSort(
                _db.Tickets.AsNoTracking(),
                q.Search, q.Status, q.CategoryId, q.Priority,
                q.DateFrom, q.DateTo, q.SortBy, q.SortDir);

            if (q.CreatedByUserId.HasValue)
            {
                qry = qry.Where(t => t.CreatedById == q.CreatedByUserId.Value);
            }

            if (q.AssignedToIsNull)
            {
                qry = qry.Where(t => t.AssignedToId == null);
            }
            else if (q.AssignedToUserId.HasValue)
            {
                qry = qry.Where(t => t.AssignedToId == q.AssignedToUserId.Value);
            }

            var rows = await qry
                .Select(t => new TicketListItemDto(
                    t.TicketId,
                    t.Title,
                    t.CategoryId,
                    (byte)t.Priority,
                    (byte)t.Status,
                    t.CreatedAt,
                    t.UpdatedAt,
                    _db.Users.Where(u => u.UserId == t.CreatedById).Select(u => u.Name).FirstOrDefault(),
                    t.AssignedToId == null
                        ? null
                        : _db.Users.Where(u => u.UserId == t.AssignedToId).Select(u => u.Name).FirstOrDefault()
                ))
                .ToListAsync(ct);

            return rows;
        }

        private IQueryable<Ticket> ApplyFiltersAndSort(
            IQueryable<Ticket> qry,
            string? search,
            byte? status, int? category, byte? priority,
            DateOnly? dateFrom, DateOnly? dateTo,
            string sortBy, string sortDir)
        {
            // search in id, title, description
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                qry = qry.Where(t =>
                    t.TicketId.ToString().Contains(term) ||
                    EF.Functions.ILike(t.Title, $"%{term}%") ||
                    EF.Functions.ILike(t.Description ?? string.Empty, $"%{term}%")
                );
            }

            if (status is > 0) qry = qry.Where(t => (int)t.Status == status);
            if (category is > 0) qry = qry.Where(t => t.CategoryId == category);
            if (priority is > 0) qry = qry.Where(t => (int)t.Priority == priority);

            if (dateFrom is not null)
            {
                var from = dateFrom.Value.ToDateTime(TimeOnly.MinValue).ToUniversalTime();
                qry = qry.Where(t => t.CreatedAt >= from);
            }
            if (dateTo is not null)
            {
                var to = dateTo.Value.ToDateTime(TimeOnly.MaxValue).ToUniversalTime();
                qry = qry.Where(t => t.CreatedAt <= to);
            }

            var key = (sortBy ?? "createdAt").Replace("_", "").ToLowerInvariant();
            var asc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

            qry = key switch
            {
                "ticketid" => asc ? qry.OrderBy(t => t.TicketId) : qry.OrderByDescending(t => t.TicketId),
                "title" => asc ? qry.OrderBy(t => t.Title) : qry.OrderByDescending(t => t.Title),
                "category" => asc ? qry.OrderBy(t => t.CategoryId) : qry.OrderByDescending(t => t.CategoryId),
                "priority" => asc ? qry.OrderBy(t => t.Priority) : qry.OrderByDescending(t => t.Priority),
                "status" => asc ? qry.OrderBy(t => t.Status) : qry.OrderByDescending(t => t.Status),
                "updatedat" => asc ? qry.OrderBy(t => t.UpdatedAt) : qry.OrderByDescending(t => t.UpdatedAt),
                _ => asc ? qry.OrderBy(t => t.CreatedAt) : qry.OrderByDescending(t => t.CreatedAt)
            };

            return qry;
        }

        /// <summary>
        /// Truncates text to max 100 characters for history logging to avoid excessively long entries.
        /// </summary>
        private static string TruncateForHistory(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            const int maxLength = 100;
            if (text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength) + "...";
        }

        public async Task<IReadOnlyList<AssignableUserDto>> GetAssignableUsersAsync(int ticketId, CancellationToken ct)
        {
            // Verify ticket exists and get its category
            var ticket = await _db.Tickets
                .AsNoTracking()
                .Where(t => t.TicketId == ticketId)
                .Select(t => new { t.TicketId, t.CategoryId })
                .FirstOrDefaultAsync(ct);

            if (ticket is null)
                throw new AppException(ErrorCodes.TicketNotFound, "Ticket not found", HttpStatusCode.NotFound);

            // Query active users with eligible roles
            var users = await _db.Users
                .AsNoTracking()
                .Include(u => u.Category)
                .Where(u => u.IsActive)
                .Where(u => u.RoleId == Enums.Identity.UserRole.Support 
                         || u.RoleId == Enums.Identity.UserRole.TeamLeader 
                         || u.RoleId == Enums.Identity.UserRole.Admin)
                // Admin exempt from category check, Support/TeamLeader must match ticket category
                .Where(u => u.RoleId == Enums.Identity.UserRole.Admin || u.CategoryId == ticket.CategoryId)
                .OrderByDescending(u => u.RoleId) // Admin (4) > TeamLeader (3) > Support (2)
                .ThenBy(u => u.Name)
                .Select(u => new AssignableUserDto(
                    u.UserId,
                    u.Name,
                    u.Email,
                    u.RoleId.ToString(),
                    u.CategoryId,
                    u.Category != null ? u.Category.NamePl : null,
                    u.Category != null ? u.Category.NameEn : null
                ))
                .ToListAsync(ct);

            return users;
        }
    }
}
