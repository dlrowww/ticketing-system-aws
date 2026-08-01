using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using TicketingSystem.Api.Data;
using TicketingSystem.Api.Enums.History;
using TicketingSystem.Api.Enums.Identity;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.Models;
using TicketingSystem.Api.SeedData.Models;
using TicketingSystem.Api.Services;
using TicketingSystem.Api.Services.Tickets;
using TicketingSystem.Api.Utils;

namespace TicketingSystem.Api.SeedData;

public static class DemoDataSeeder
{
    private const string DemoPassword = "IronPack2026!";
    private const string DataFilePath = "SeedData/full-seed-data.json";  // Default
    private const string AttachmentsSourcePath = "docs/AttachementFiles";
    private const string TestDataReportPath = "TEST-DATA-REFERENCE.txt";
    private const string EmailSummaryPath = "EMAIL-SUMMARY.txt";

    private static readonly List<string> _emailLog = new();
    private static readonly StringBuilder _testReport = new();

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<AppDbContext>();
        var env = serviceProvider.GetRequiredService<IWebHostEnvironment>();
        
        // CRITICAL: Check if data already seeded - prevent duplicates!
        var existingTicketCount = await db.Tickets.CountAsync();
        if (existingTicketCount > 0)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("  DEMO DATA ALREADY SEEDED");
            Console.WriteLine("==================================================");
            Console.WriteLine($"✓ Found {existingTicketCount} existing tickets");
            Console.WriteLine($"✓ Skipping seeding to prevent duplicates");
            Console.WriteLine($"✓ To re-seed: Delete all data first or drop/recreate database\n");
            return;
        }
        
        Console.WriteLine("==================================================");
        Console.WriteLine("  DEMO DATA SEEDING - IronPack Ticketing System");
        Console.WriteLine("==================================================\n");

        // Load JSON data (support environment variable override)
        var seedFileName = Environment.GetEnvironmentVariable("SEED_DATA_FILE") ?? DataFilePath;
        var dataPath = Path.Combine(env.ContentRootPath, seedFileName);
        if (!File.Exists(dataPath))
        {
            throw new FileNotFoundException($"Demo data file not found: {dataPath}");
        }

        Console.WriteLine($"✓ Using seed file: {seedFileName}\n");

        var json = await File.ReadAllTextAsync(dataPath);
        var data = JsonSerializer.Deserialize<DemoDataRoot>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize demo data");

        Console.WriteLine($"✓ Loaded demo data: {data.Users.Count} users, {data.Tickets.Count} tickets\n");

        // Initialize test report
        _testReport.AppendLine("==============================================");
        _testReport.AppendLine(" DEMO DATA TEST REFERENCE GUIDE");
        _testReport.AppendLine("==============================================");
        _testReport.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

        // Step 1: Seed Categories
        await SeedCategoriesAsync(db, data.Categories);

        // Step 2: Seed Users
        var userMap = await SeedUsersAsync(db, data.Users);

        // Step 3: Seed Tickets (with attachments and comments)
        await SeedTicketsAsync(serviceProvider, db, env, data.Tickets, userMap);

        // Step 4: Generate reports
        await GenerateTestReportAsync(env, db, userMap);
        await GenerateEmailSummaryAsync(env);

        Console.WriteLine("\n==================================================");
        Console.WriteLine("  SEEDING COMPLETED SUCCESSFULLY");
        Console.WriteLine("==================================================");
        Console.WriteLine($"✓ Reports generated:");
        Console.WriteLine($"  - {TestDataReportPath}");
        Console.WriteLine($"  - {EmailSummaryPath}");
        Console.WriteLine($"✓ Total emails logged: {_emailLog.Count}");
        Console.WriteLine($"✓ Demo password for all users: {DemoPassword}\n");
    }

    private static async Task SeedCategoriesAsync(AppDbContext db, List<DemoCategory> categories)
    {
        Console.WriteLine("1. Seeding Categories...");

        foreach (var cat in categories)
        {
            if (!await db.Categories.AnyAsync(c => c.CategoryId == cat.CategoryId))
            {
                db.Categories.Add(new Category
                {
                    CategoryId = cat.CategoryId,
                    NamePl = cat.NamePl,
                    NameEn = cat.NameEn,
                    IsActive = cat.IsActive
                });
                Console.WriteLine($"   ✓ Created category: {cat.NameEn} ({cat.NamePl})");
            }
        }

        await db.SaveChangesAsync();
        Console.WriteLine();
    }

    private static async Task<Dictionary<string, User>> SeedUsersAsync(AppDbContext db, List<DemoUser> users)
    {
        Console.WriteLine("2. Seeding Users...");

        var userMap = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);
        var categories = await db.Categories.ToDictionaryAsync(c => c.NameEn, c => c.CategoryId);

        foreach (var demoUser in users)
        {
            var normalizedEmail = demoUser.Email.Trim().ToLowerInvariant();
            var existing = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (existing != null)
            {
                userMap[demoUser.Email] = existing;
                continue;
            }

            var user = new User
            {
                Name = demoUser.Name,
                Email = demoUser.Email,
                PasswordHash = PasswordHasher.Hash(DemoPassword),
                RoleId = Enum.Parse<UserRole>(demoUser.Role),
                CategoryId = !string.IsNullOrEmpty(demoUser.Category) ? categories[demoUser.Category] : null,
                IsActive = true
            };

            db.Users.Add(user);
            await db.SaveChangesAsync(); // Save immediately to get UserId
            userMap[demoUser.Email] = user;

            Console.WriteLine($"   ✓ Created user: {user.Name} ({user.Email}) - {demoUser.Role}");
        }

        Console.WriteLine();
        return userMap;
    }

    private static async Task SeedTicketsAsync(
        IServiceProvider serviceProvider,
        AppDbContext db,
        IWebHostEnvironment env,
        List<DemoTicket> tickets,
        Dictionary<string, User> userMap)
    {
        Console.WriteLine("3. Seeding Tickets...");

        var categories = await db.Categories.ToDictionaryAsync(c => c.NameEn, c => c.CategoryId);
        var ticketService = serviceProvider.GetRequiredService<ITicketService>();
        var attachmentService = serviceProvider.GetRequiredService<ITicketAttachmentService>();
        var commentService = serviceProvider.GetRequiredService<ICommentService>();
        var emailService = serviceProvider.GetRequiredService<Services.Email.IEmailService>();

        var now = DateTime.UtcNow;
        var ticketNumber = 1;

        foreach (var demoTicket in tickets)
        {
            var creator = userMap[demoTicket.CreatorEmail];
            var assignee = !string.IsNullOrEmpty(demoTicket.AssigneeEmail) 
                ? userMap[demoTicket.AssigneeEmail] 
                : null;

            var createdAt = now.AddDays(-demoTicket.CreatedDaysAgo);
            var resolvedAt = demoTicket.ResolvedDaysAgo.HasValue 
                ? now.AddDays(-demoTicket.ResolvedDaysAgo.Value) 
                : (DateTime?)null;

            // Create ticket directly in DB (bypass service to control timestamps)
            var ticket = new Ticket
            {
                Title = demoTicket.Title,
                Description = demoTicket.Description,
                CategoryId = categories[demoTicket.Category],
                Priority = Enum.Parse<TicketPriority>(demoTicket.Priority),
                Status = Enum.Parse<TicketStatus>(demoTicket.Status),
                CreatedAt = createdAt,
                UpdatedAt = resolvedAt ?? createdAt,
                CreatedById = creator.UserId,
                AssignedToId = assignee?.UserId
            };

            db.Tickets.Add(ticket);
            await db.SaveChangesAsync(); // Get TicketId

            Console.WriteLine($"   [{ticketNumber}/{tickets.Count}] Created ticket #{ticket.TicketId}: {ticket.Title.Substring(0, Math.Min(50, ticket.Title.Length))}...");

            // SPECIAL HANDLING: Complex workflow ticket for email notification testing
            if (demoTicket.IsComplexWorkflowTest)
            {
                await SeedComplexWorkflowTicket(db, emailService, ticket, demoTicket, userMap, creator, assignee, createdAt, resolvedAt);
                ticketNumber++;
                continue; // Skip normal processing
            }

            // Add ticket history (creation)
            db.TicketHistories.Add(new TicketHistory
            {
                TicketId = ticket.TicketId,
                ChangeType = HistoryChangeType.TicketCreated,
                OldValue = null,
                NewValue = $"Status: {ticket.Status} | Category: {demoTicket.Category} | Priority: {ticket.Priority}",
                ChangedById = creator.UserId,
                ChangedAt = createdAt
            });

            // Add assignment history
            if (assignee != null)
            {
                db.TicketHistories.Add(new TicketHistory
                {
                    TicketId = ticket.TicketId,
                    ChangeType = HistoryChangeType.AssignmentChanged,
                    OldValue = null,
                    NewValue = assignee.UserId.ToString(),
                    ChangedById = creator.UserId,
                    ChangedAt = createdAt.AddMinutes(1)
                });

                // Log and SEND email for assignment
                LogEmail($"TicketAssigned → {assignee.Email} (Ticket #{ticket.TicketId})");
                try
                {
                    await emailService.SendTicketAssignedAsync(ticket.TicketId, assignee.UserId, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"      ⚠️ Failed to send assignment email: {ex.Message}");
                }
            }

            await db.SaveChangesAsync();

            // Add comments
            foreach (var demoComment in demoTicket.Comments)
            {
                var commentAuthor = userMap[demoComment.AuthorEmail];
                var commentDate = createdAt.AddDays(demoComment.DaysAgoFromCreation);

                var comment = new TicketComment
                {
                    TicketId = ticket.TicketId,
                    Content = demoComment.Content,
                    IsInternal = demoComment.IsInternal,
                    CreatedById = commentAuthor.UserId,
                    CreatedAt = commentDate
                };

                db.TicketComments.Add(comment);
                await db.SaveChangesAsync();

                // Add comment history
                db.TicketHistories.Add(new TicketHistory
                {
                    TicketId = ticket.TicketId,
                    ChangeType = HistoryChangeType.CommentAdded,
                    OldValue = null,
                    NewValue = $"Comment #{comment.CommentId}",
                    ChangedById = commentAuthor.UserId,
                    ChangedAt = commentDate
                });

                // Log and SEND email for public comments (exclude commenter)
                if (!demoComment.IsInternal)
                {
                    var recipients = new List<string> { creator.Email };
                    if (assignee != null && assignee.Email != commentAuthor.Email)
                    {
                        recipients.Add(assignee.Email);
                    }
                    recipients = recipients.Where(e => e != commentAuthor.Email).Distinct().ToList();
                    
                    if (recipients.Any())
                    {
                        LogEmail($"CommentAdded → {string.Join(", ", recipients)} (Ticket #{ticket.TicketId})");
                        try
                        {
                            // Send email to all relevant users
                            var allUsers = new[] { creator, assignee }.Where(u => u != null && u.Email != commentAuthor.Email).Select(u => u!.UserId).Distinct();
                            foreach (var userId in allUsers)
                            {
                                await emailService.SendCommentAddedAsync(ticket.TicketId, comment.CommentId, userId, CancellationToken.None);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"      ⚠️ Failed to send comment email: {ex.Message}");
                        }
                    }
                }
            }

            // Add attachments (real file uploads)
            if (demoTicket.Attachments.Any())
            {
                await AddAttachmentsAsync(env, attachmentService, ticket.TicketId, demoTicket.Attachments, creator.UserId);
            }

            // Add resolution history if resolved
            if (ticket.Status == TicketStatus.Resolved && resolvedAt.HasValue)
            {
                db.TicketHistories.Add(new TicketHistory
                {
                    TicketId = ticket.TicketId,
                    ChangeType = HistoryChangeType.StatusChanged,
                    OldValue = ((int)TicketStatus.InProcess).ToString(),
                    NewValue = ((int)TicketStatus.Resolved).ToString(),
                    ChangedById = assignee?.UserId ?? creator.UserId,
                    ChangedAt = resolvedAt.Value
                });

                // Log and SEND email for resolution
                LogEmail($"TicketResolved → {creator.Email} (Ticket #{ticket.TicketId})");
                try
                {
                    await emailService.SendTicketResolvedAsync(ticket.TicketId, creator.UserId, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"      ⚠️ Failed to send resolution email: {ex.Message}");
                }
            }

            await db.SaveChangesAsync();
            ticketNumber++;
        }

        Console.WriteLine();
    }

    private static async Task AddAttachmentsAsync(
        IWebHostEnvironment env,
        ITicketAttachmentService attachmentService,
        int ticketId,
        List<string> filenames,
        int uploadedBy)
    {
        var sourcePath = Path.Combine(env.ContentRootPath, "..", "..", AttachmentsSourcePath);
        var formFiles = new List<IFormFile>();

        foreach (var filename in filenames)
        {
            var filePath = Path.Combine(sourcePath, filename);
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"      ⚠ Warning: File not found: {filename}");
                continue;
            }

            var bytes = await File.ReadAllBytesAsync(filePath);
            var stream = new MemoryStream(bytes);
            var formFile = new FormFile(stream, 0, bytes.Length, "file", filename)
            {
                Headers = new HeaderDictionary(),
                ContentType = GetContentType(filename)
            };

            formFiles.Add(formFile);
        }

        if (formFiles.Any())
        {
            // Use production service to ensure proper storage
            await attachmentService.AddAsync(ticketId, formFiles.ToArray(), uploadedBy, CancellationToken.None);
            Console.WriteLine($"      ✓ Uploaded {formFiles.Count} attachment(s)");
        }
    }

    private static string GetContentType(string filename)
    {
        var ext = Path.GetExtension(filename).ToLowerInvariant();
        return ext switch
        {
            ".txt" => "text/plain",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Seeds a complex workflow ticket that tests ALL 6 email notification templates
    /// with proper state transitions, reassignment, and priority escalation.
    /// </summary>
    private static async Task SeedComplexWorkflowTicket(
        AppDbContext db,
        Services.Email.IEmailService emailService,
        Ticket ticket,
        DemoTicket demoTicket,
        Dictionary<string, User> userMap,
        User creator,
        User? finalAssignee,
        DateTime createdAt,
        DateTime? resolvedAt)
    {
        Console.WriteLine($"      🔔 Complex Workflow Ticket - Testing all 6 email templates...");

        var initialAssignee = !string.IsNullOrEmpty(demoTicket.InitialAssigneeEmail)
            ? userMap[demoTicket.InitialAssigneeEmail]
            : null;

        var initialPriority = !string.IsNullOrEmpty(demoTicket.InitialPriority)
            ? Enum.Parse<TicketPriority>(demoTicket.InitialPriority)
            : TicketPriority.Low;

        // Timeline for workflow (spread over 2 days)
        var timeline = new
        {
            Creation = createdAt,
            FirstComment = createdAt.AddHours(2),
            PriorityEscalation = createdAt.AddHours(4),
            StatusToOpen = createdAt.AddHours(5),
            Reassignment = createdAt.AddDays(1),
            SecondComment = createdAt.AddDays(1).AddHours(1),
            StatusToInProcess = createdAt.AddDays(1).AddHours(2),
            Resolution = resolvedAt ?? createdAt.AddDays(2)
        };

        // 1. TICKET CREATED (with initial priority Low, initial assignee)
        db.TicketHistories.Add(new TicketHistory
        {
            TicketId = ticket.TicketId,
            ChangeType = HistoryChangeType.TicketCreated,
            OldValue = null,
            NewValue = $"Status: New | Category: {demoTicket.Category} | Priority: {initialPriority}",
            ChangedById = creator.UserId,
            ChangedAt = timeline.Creation
        });

        // 2. AUTO-ASSIGNMENT (to initial assignee)
        if (initialAssignee != null)
        {
            db.TicketHistories.Add(new TicketHistory
            {
                TicketId = ticket.TicketId,
                ChangeType = HistoryChangeType.AssignmentChanged,
                OldValue = null,
                NewValue = initialAssignee.UserId.ToString(),
                ChangedById = creator.UserId,
                ChangedAt = timeline.Creation.AddMinutes(1)
            });

            // ✉️ EMAIL 1: TicketAssigned
            LogEmail($"TicketAssigned → {initialAssignee.Email} (Ticket #{ticket.TicketId})");
            await emailService.SendTicketAssignedAsync(ticket.TicketId, initialAssignee.UserId, CancellationToken.None);
        }

        // 3. FIRST COMMENT (by initial assignee)
        if (demoTicket.Comments.Any() && initialAssignee != null)
        {
            var firstComment = demoTicket.Comments[0];
            var comment1 = new TicketComment
            {
                TicketId = ticket.TicketId,
                Content = firstComment.Content,
                IsInternal = firstComment.IsInternal,
                CreatedById = initialAssignee.UserId,
                CreatedAt = timeline.FirstComment
            };
            db.TicketComments.Add(comment1);
            await db.SaveChangesAsync();

            // Add comment history
            db.TicketHistories.Add(new TicketHistory
            {
                TicketId = ticket.TicketId,
                ChangeType = HistoryChangeType.CommentAdded,
                OldValue = null,
                NewValue = $"Comment #{comment1.CommentId}",
                ChangedById = initialAssignee.UserId,
                ChangedAt = timeline.FirstComment
            });

            if (!firstComment.IsInternal)
            {
                // ✉️ EMAIL 2: CommentAdded (to creator only, exclude commenter)
                LogEmail($"CommentAdded → {creator.Email} (Ticket #{ticket.TicketId})");
                await emailService.SendCommentAddedAsync(ticket.TicketId, comment1.CommentId, initialAssignee.UserId, CancellationToken.None);
            }
        }

        // 4. PRIORITY ESCALATION (Low → High/Critical)
        if (initialPriority != ticket.Priority)
        {
            db.TicketHistories.Add(new TicketHistory
            {
                TicketId = ticket.TicketId,
                ChangeType = HistoryChangeType.PriorityChanged,
                OldValue = ((int)initialPriority).ToString(),
                NewValue = ((int)ticket.Priority).ToString(),
                ChangedById = initialAssignee?.UserId ?? creator.UserId,
                ChangedAt = timeline.PriorityEscalation
            });

            // ✉️ EMAIL 3: PriorityEscalated (to creator + assignee)
            var recipients3 = new List<string> { creator.Email };
            if (initialAssignee != null)
            {
                recipients3.Add(initialAssignee.Email);
            }
            LogEmail($"PriorityEscalated → {string.Join(", ", recipients3.Distinct())} (Ticket #{ticket.TicketId})");
            await emailService.SendPriorityEscalatedAsync(ticket.TicketId, (byte)initialPriority, (byte)ticket.Priority, initialAssignee?.UserId ?? creator.UserId, CancellationToken.None);
        }

        // 5. STATUS CHANGE: New → Open
        db.TicketHistories.Add(new TicketHistory
        {
            TicketId = ticket.TicketId,
            ChangeType = HistoryChangeType.StatusChanged,
            OldValue = ((int)TicketStatus.New).ToString(),
            NewValue = ((int)TicketStatus.Open).ToString(),
            ChangedById = initialAssignee?.UserId ?? creator.UserId,
            ChangedAt = timeline.StatusToOpen
        });

        // ✉️ EMAIL 4: TicketStatusChanged (to creator + assignee)
        var recipients4 = new List<string> { creator.Email };
        if (initialAssignee != null)
        {
            recipients4.Add(initialAssignee.Email);
        }
        LogEmail($"TicketStatusChanged → {string.Join(", ", recipients4.Distinct())} (Ticket #{ticket.TicketId})");
        await emailService.SendTicketStatusChangedAsync(ticket.TicketId, (byte)TicketStatus.New, (byte)TicketStatus.Open, initialAssignee?.UserId ?? creator.UserId, CancellationToken.None);

        // 6. REASSIGNMENT (initial assignee → final assignee)
        if (initialAssignee != null && finalAssignee != null && initialAssignee.UserId != finalAssignee.UserId)
        {
            db.TicketHistories.Add(new TicketHistory
            {
                TicketId = ticket.TicketId,
                ChangeType = HistoryChangeType.AssignmentChanged,
                OldValue = initialAssignee.UserId.ToString(),
                NewValue = finalAssignee.UserId.ToString(),
                ChangedById = initialAssignee.UserId,
                ChangedAt = timeline.Reassignment
            });

            // ✉️ EMAIL 5: TicketReassigned (to old assignee + new assignee)
            LogEmail($"TicketReassigned → {initialAssignee.Email}, {finalAssignee.Email} (Ticket #{ticket.TicketId})");
            await emailService.SendTicketReassignedAsync(ticket.TicketId, initialAssignee.UserId, finalAssignee.UserId, initialAssignee.UserId, CancellationToken.None);
        }

        // 7. SECOND COMMENT (by new assignee)
        if (demoTicket.Comments.Count > 1 && finalAssignee != null)
        {
            var secondComment = demoTicket.Comments[1];
            var comment2 = new TicketComment
            {
                TicketId = ticket.TicketId,
                Content = secondComment.Content,
                IsInternal = secondComment.IsInternal,
                CreatedById = finalAssignee.UserId,
                CreatedAt = timeline.SecondComment
            };
            db.TicketComments.Add(comment2);
            await db.SaveChangesAsync();

            // Add comment history
            db.TicketHistories.Add(new TicketHistory
            {
                TicketId = ticket.TicketId,
                ChangeType = HistoryChangeType.CommentAdded,
                OldValue = null,
                NewValue = $"Comment #{comment2.CommentId}",
                ChangedById = finalAssignee.UserId,
                ChangedAt = timeline.SecondComment
            });

            if (!secondComment.IsInternal)
            {
                // ✉️ EMAIL 6: CommentAdded (to creator + old assignee, exclude new assignee commenter)
                var recipients6 = new List<string> { creator.Email };
                if (initialAssignee != null && initialAssignee.Email != finalAssignee.Email)
                {
                    recipients6.Add(initialAssignee.Email);
                }
                LogEmail($"CommentAdded → {string.Join(", ", recipients6.Distinct())} (Ticket #{ticket.TicketId})");
                await emailService.SendCommentAddedAsync(ticket.TicketId, comment2.CommentId, finalAssignee.UserId, CancellationToken.None);
            }
        }

        // 8. STATUS CHANGE: Open → InProcess
        db.TicketHistories.Add(new TicketHistory
        {
            TicketId = ticket.TicketId,
            ChangeType = HistoryChangeType.StatusChanged,
            OldValue = ((int)TicketStatus.Open).ToString(),
            NewValue = ((int)TicketStatus.InProcess).ToString(),
            ChangedById = finalAssignee?.UserId ?? creator.UserId,
            ChangedAt = timeline.StatusToInProcess
        });

        // ✉️ EMAIL 7: TicketStatusChanged (to creator + new assignee)
        var recipients7 = new List<string> { creator.Email };
        if (finalAssignee != null)
        {
            recipients7.Add(finalAssignee.Email);
        }
        LogEmail($"TicketStatusChanged → {string.Join(", ", recipients7.Distinct())} (Ticket #{ticket.TicketId})");
        await emailService.SendTicketStatusChangedAsync(ticket.TicketId, (byte)TicketStatus.Open, (byte)TicketStatus.InProcess, finalAssignee?.UserId ?? creator.UserId, CancellationToken.None);

        // 9. STATUS CHANGE: InProcess → Resolved
        if (ticket.Status == TicketStatus.Resolved)
        {
            db.TicketHistories.Add(new TicketHistory
            {
                TicketId = ticket.TicketId,
                ChangeType = HistoryChangeType.StatusChanged,
                OldValue = ((int)TicketStatus.InProcess).ToString(),
                NewValue = ((int)TicketStatus.Resolved).ToString(),
                ChangedById = finalAssignee?.UserId ?? creator.UserId,
                ChangedAt = timeline.Resolution
            });

            // ✉️ EMAIL 8: TicketResolved (to creator only)
            LogEmail($"TicketResolved → {creator.Email} (Ticket #{ticket.TicketId})");
            await emailService.SendTicketResolvedAsync(ticket.TicketId, finalAssignee?.UserId ?? creator.UserId, CancellationToken.None);
        }

        await db.SaveChangesAsync();
        Console.WriteLine($"      ✓ Created 8-step workflow with 8 email notifications");
    }

    private static void LogEmail(string message)
    {
        _emailLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
    }

    private static async Task GenerateTestReportAsync(IWebHostEnvironment env, AppDbContext db, Dictionary<string, User> userMap)
    {
        _testReport.AppendLine("LOGIN CREDENTIALS");
        _testReport.AppendLine("=================");
        _testReport.AppendLine($"Password for ALL users: {DemoPassword}\n");

        foreach (var kvp in userMap.OrderBy(u => u.Value.UserId))
        {
            var user = kvp.Value;
            var ticketCount = await db.Tickets.CountAsync(t => t.AssignedToId == user.UserId || t.CreatedById == user.UserId);
            var category = user.CategoryId.HasValue 
                ? (await db.Categories.FindAsync(user.CategoryId.Value))?.NameEn ?? "N/A"
                : "N/A";

            _testReport.AppendLine($"User #{user.UserId}: {user.Name}");
            _testReport.AppendLine($"  Email: {user.Email}");
            _testReport.AppendLine($"  Role: {user.RoleId}");
            _testReport.AppendLine($"  Category: {category}");
            _testReport.AppendLine($"  Tickets: {ticketCount}");
            _testReport.AppendLine();
        }

        _testReport.AppendLine("\nUSERS FOR TESTING EMPTY STATES");
        _testReport.AppendLine("===============================");
        
        var usersWithoutTickets = await db.Users
            .Where(u => !db.Tickets.Any(t => t.AssignedToId == u.UserId || t.CreatedById == u.UserId))
            .ToListAsync();

        foreach (var user in usersWithoutTickets)
        {
            _testReport.AppendLine($"  • {user.Name} ({user.Email}) - {user.RoleId} - 0 tickets");
        }

        _testReport.AppendLine("\n\nTICKETS BY STATUS");
        _testReport.AppendLine("=================");
        
        var statusGroups = await db.Tickets
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var group in statusGroups.OrderBy(g => g.Status))
        {
            var percentage = (group.Count * 100.0 / await db.Tickets.CountAsync());
            _testReport.AppendLine($"  {group.Status}: {group.Count} tickets ({percentage:F1}%)");
        }

        _testReport.AppendLine("\n\nTICKETS BY ATTACHMENT COUNT");
        _testReport.AppendLine("============================");
        
        var ticketsWithAttachments = await db.Tickets
            .Select(t => new
            {
                t.TicketId,
                t.Title,
                AttachmentCount = db.TicketFiles.Count(f => f.TicketId == t.TicketId)
            })
            .ToListAsync();

        _testReport.AppendLine($"  0 attachments: {ticketsWithAttachments.Count(t => t.AttachmentCount == 0)} tickets");
        _testReport.AppendLine($"  1-4 attachments: {ticketsWithAttachments.Count(t => t.AttachmentCount >= 1 && t.AttachmentCount <= 4)} tickets");
        _testReport.AppendLine($"  5-10 attachments: {ticketsWithAttachments.Count(t => t.AttachmentCount >= 5 && t.AttachmentCount <= 10)} tickets");
        _testReport.AppendLine($"  11+ attachments: {ticketsWithAttachments.Count(t => t.AttachmentCount >= 11)} tickets");

        _testReport.AppendLine("\n\nHIGH ATTACHMENT COUNT TICKETS (for stress testing)");
        _testReport.AppendLine("===================================================");
        
        var highAttachmentTickets = ticketsWithAttachments
            .Where(t => t.AttachmentCount >= 5)
            .OrderByDescending(t => t.AttachmentCount)
            .Take(10);

        foreach (var t in highAttachmentTickets)
        {
            _testReport.AppendLine($"  Ticket #{t.TicketId}: {t.AttachmentCount} files - {t.Title.Substring(0, Math.Min(60, t.Title.Length))}...");
        }

        _testReport.AppendLine("\n\nRESOLUTION TIME TESTING");
        _testReport.AppendLine("=======================");
        
        var resolvedTickets = await db.Tickets
            .Where(t => t.Status == TicketStatus.Resolved && t.UpdatedAt.HasValue)
            .Select(t => new
            {
                t.TicketId,
                t.Title,
                t.CreatedAt,
                t.UpdatedAt
            })
            .ToListAsync();
        
        var resolvedWithTime = resolvedTickets
            .Select(t => new
            {
                t.TicketId,
                t.Title,
                ResolutionTime = (int)(t.UpdatedAt!.Value - t.CreatedAt).TotalHours
            })
            .OrderBy(t => t.ResolutionTime)
            .ToList();

        if (resolvedWithTime.Any())
        {
            var avgHours = resolvedWithTime.Average(t => t.ResolutionTime);
            _testReport.AppendLine($"  Average resolution time: {avgHours:F1} hours ({avgHours / 24:F1} days)");
            _testReport.AppendLine($"\n  Fastest resolutions:");
            
            foreach (var t in resolvedWithTime.Take(3))
            {
                _testReport.AppendLine($"    Ticket #{t.TicketId}: {t.ResolutionTime} hours ({t.ResolutionTime / 24.0:F1} days)");
            }

            _testReport.AppendLine($"\n  Slowest resolutions:");
            
            foreach (var t in resolvedWithTime.OrderByDescending(t => t.ResolutionTime).Take(3))
            {
                _testReport.AppendLine($"    Ticket #{t.TicketId}: {t.ResolutionTime} hours ({t.ResolutionTime / 24.0:F1} days)");
            }
        }

        _testReport.AppendLine("\n\nTESTING SCENARIOS");
        _testReport.AppendLine("=================");
        _testReport.AppendLine("1. Empty States:");
        _testReport.AppendLine("   - Login with users who have 0 tickets");
        _testReport.AppendLine("   - Verify empty list views, dashboards show zeros");
        _testReport.AppendLine();
        _testReport.AppendLine("2. Attachment Download:");
        _testReport.AppendLine("   - Test tickets with various attachment counts");
        _testReport.AppendLine("   - Verify all file types download correctly (.txt, .png, .jpg, .pdf, .zip)");
        _testReport.AppendLine();
        _testReport.AppendLine("3. Email Notifications:");
        _testReport.AppendLine($"   - Check EMAIL-SUMMARY.txt for {_emailLog.Count} logged emails");
        _testReport.AppendLine("   - Verify emails in C:\\Dev\\EmailPickup (if using pickup directory)");
        _testReport.AppendLine();
        _testReport.AppendLine("4. Resolution Time Dashboard:");
        _testReport.AppendLine("   - Verify average resolution time calculation");
        _testReport.AppendLine("   - Check tickets resolved in <24h vs >30 days");
        _testReport.AppendLine();
        _testReport.AppendLine("5. Comments (Public vs Internal):");
        _testReport.AppendLine("   - Verify internal comments visible only to support staff");
        _testReport.AppendLine("   - Public comments visible to ticket creator");

        var reportPath = Path.Combine(env.ContentRootPath, TestDataReportPath);
        await File.WriteAllTextAsync(reportPath, _testReport.ToString());
        
        Console.WriteLine($"4. Generated test reference: {TestDataReportPath}");
    }

    private static async Task GenerateEmailSummaryAsync(IWebHostEnvironment env)
    {
        var summary = new StringBuilder();
        summary.AppendLine("===================================");
        summary.AppendLine(" EMAIL NOTIFICATION SUMMARY");
        summary.AppendLine("===================================");
        summary.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        summary.AppendLine($"Total emails logged: {_emailLog.Count}\n");
        summary.AppendLine("EMAIL LOG:");
        summary.AppendLine("==========\n");

        foreach (var email in _emailLog)
        {
            summary.AppendLine(email);
        }

        summary.AppendLine("\n\nNOTE:");
        summary.AppendLine("If using pickup directory (Development), check: C:\\Dev\\EmailPickup");
        summary.AppendLine("Email filenames include timestamp and recipient for easy identification.");

        var reportPath = Path.Combine(env.ContentRootPath, EmailSummaryPath);
        await File.WriteAllTextAsync(reportPath, summary.ToString());
        
        Console.WriteLine($"5. Generated email summary: {EmailSummaryPath}");
    }
}
