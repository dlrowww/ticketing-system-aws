using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using TicketingSystem.Api.Common;
using TicketingSystem.Api.DTOs.Tickets;
using TicketingSystem.Api.Enums.Identity;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.IntegrationTests.Helpers;
using TicketingSystem.Api.Models;
using TicketingSystem.Api.Services;
using Xunit;

namespace TicketingSystem.Api.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.CollectionName)]
public sealed class TicketsControllerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly PostgresTestContainer _postgres;

    public TicketsControllerTests(PostgresTestContainer postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task CreateTicket_WithValidRequest_Returns201AndCreatedTicket()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        const string email = "employee@example.com";
        const string password = "Employee#123";

        var employeeId = await SeedUserAsync(ctx, "Employee", email, password, UserRole.Employee, 1); // IT category
        var leaderId = await SeedUserAsync(ctx, "Team Lead", "leader@example.com", "Leader#123", UserRole.TeamLeader, 1); // IT category

        await AuthenticateAsync(ctx, email, password);

        using var form = BuildTicketForm(
            title: "Laptop fails to boot",
            description: "The laptop consistently fails to boot after the latest update.",
            category: 1, // IT
            priority: TicketPriority.High);

        var response = await ctx.Client.PostAsync("/api/tickets", form);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateTicketResponse>(JsonOptions);
        created.Should().NotBeNull();
        created!.TicketId.Should().BeGreaterThan(0);
        created.Status.Should().Be(TicketStatus.New);
        created.AssignedToUserId.Should().Be(leaderId);

        await ctx.WithDbContextAsync(async db =>
        {
            var ticket = await db.Tickets.FindAsync(created.TicketId);
            ticket.Should().NotBeNull();
            ticket!.Title.Should().Be("Laptop fails to boot");
            ticket.CreatedById.Should().Be(employeeId);
            ticket.AssignedToId.Should().Be(leaderId);
        });
    }

    [Fact]
    public async Task CreateTicket_WithInvalidTitle_Returns400()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        const string email = "employee@example.com";
        const string password = "Employee#123";

        await SeedUserAsync(ctx, "Employee", email, password, UserRole.Employee, 1); // IT category
        await AuthenticateAsync(ctx, email, password);

        using var form = BuildTicketForm(
            title: "Bad",
            description: "Description is long enough to pass validation requirements.",
            category: 1, // IT
            priority: TicketPriority.Medium);

        var response = await ctx.Client.PostAsync("/api/tickets", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertValidationFieldErrorAsync(
            response,
            expectedTopLevelCode: ErrorCodes.ValidationFailed,
            fieldName: "Title",
            expectedFieldErrorCode: ErrorCodes.TicketTitleTooShort);
    }

    [Fact]
    public async Task CreateTicket_WithoutAuthentication_Returns401()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        using var form = BuildTicketForm(
            title: "Network outage",
            description: "Building wide network outage after power spike detected.",
            category: 1, // IT
            priority: TicketPriority.Critical);

        var response = await ctx.Client.PostAsync("/api/tickets", form);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateTicket_WithValidRequest_ReturnsUpdatedTicket()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var employeeId = await SeedUserAsync(ctx, "Employee", "employee@example.com", "Employee#123", UserRole.Employee, 1); // IT category
        var assigneeId = await SeedUserAsync(ctx, "Support", "support@example.com", "Support#123", UserRole.Support, 1); // IT category

        var ticketId = await SeedTicketAsync(ctx, CreateTicket(
            createdById: employeeId,
            category: 1, // IT
            priority: TicketPriority.Medium,
            status: TicketStatus.New,
            title: "Printer malfunction",
            description: "Office printer jams on every second page and requires maintenance.",
            assignedToId: assigneeId));

        // Priority/Status are restricted fields; authenticate as Support (allowed).
        await AuthenticateAsync(ctx, "support@example.com", "Support#123");

        var update = new UpdateTicketRequest
        {
            Title = "Printer malfunction escalated",
            Description = "Printer is now completely offline and blocking urgent document printing.",
            Priority = TicketPriority.Critical,
            Status = TicketStatus.Open
        };

        var response = await ctx.Client.PatchAsJsonAsync($"/api/tickets/{ticketId}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var details = await response.Content.ReadFromJsonAsync<TicketDetailsDto>(JsonOptions);
        details.Should().NotBeNull();
        details!.Title.Should().Be("Printer malfunction escalated");
        details.Status.Should().Be((int)TicketStatus.Open);
        details.Priority.Should().Be((int)TicketPriority.Critical);
        details.AssignedToId.Should().Be(assigneeId);
        details.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateTicket_WithInvalidStatusTransition_Returns409Conflict()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        const string email = "employee@example.com";
        const string password = "Employee#123";

        var employeeId = await SeedUserAsync(ctx, "Employee", email, password, UserRole.Employee, 1); // IT category
        await SeedUserAsync(ctx, "Support", "support@example.com", "Support#123", UserRole.Support, 1);

        var ticketId = await SeedTicketAsync(ctx, CreateTicket(
            createdById: employeeId,
            category: 1, // IT
            priority: TicketPriority.Medium,
            status: TicketStatus.InProcess,
            title: "In-process incident",
            description: "Incident in progress but used to test invalid transition."));

        // Status updates are restricted; authenticate as Support (allowed).
        await AuthenticateAsync(ctx, "support@example.com", "Support#123");

        var update = new UpdateTicketRequest
        {
            Status = TicketStatus.New
        };

        var response = await ctx.Client.PatchAsJsonAsync($"/api/tickets/{ticketId}", update);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await AssertProblemCodeAsync(response, ErrorCodes.TicketStatusTransitionInvalid);
    }

    [Fact]
    public async Task UpdateTicket_NonExistingTicket_Returns404()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        const string email = "employee@example.com";
        const string password = "Employee#123";

        await SeedUserAsync(ctx, "Employee", email, password, UserRole.Employee, 1); // IT category
        await AuthenticateAsync(ctx, email, password);

        var update = new UpdateTicketRequest
        {
            Title = "Updated title",
            Description = "Updated description with sufficient length for validation.",
            Priority = TicketPriority.High
        };

        var response = await ctx.Client.PatchAsJsonAsync("/api/tickets/9999", update);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await AssertProblemCodeAsync(response, ErrorCodes.TicketNotFound);
    }

    #region Edit Permission Tests

    [Fact]
    public async Task UpdateTicket_AdminCanEditAllFields_Returns200()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var adminId = await SeedUserAsync(ctx, "Admin", "admin@example.com", "Admin#123", UserRole.Admin);
        var employeeId = await SeedUserAsync(ctx, "Employee", "employee@example.com", "Employee#123", UserRole.Employee, 1);

        var ticketId = await SeedTicketAsync(ctx, CreateTicket(
            createdById: employeeId,
            category: 1,
            priority: TicketPriority.Medium,
            status: TicketStatus.Open,
            title: "Original Title",
            description: "Original Description"));

        await AuthenticateAsync(ctx, "admin@example.com", "Admin#123");

        var update = new UpdateTicketRequest
        {
            Title = "Admin Updated Title",
            Description = "Admin updated description with sufficient length.",
            CategoryId = 2, // Change category
            Priority = TicketPriority.Critical, // Change priority
            Status = TicketStatus.Open // Change status
        };

        var response = await ctx.Client.PatchAsJsonAsync($"/api/tickets/{ticketId}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TicketDetailsDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.Title.Should().Be("Admin Updated Title");
        result.CategoryId.Should().Be(2);
        result.Priority.Should().Be((int)TicketPriority.Critical);
        result.Status.Should().Be((int)TicketStatus.Open);
    }

    [Fact]
    public async Task UpdateTicket_EmployeeCanEditTitleAndDescriptionInMutableStatus_Returns200()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var employeeId = await SeedUserAsync(ctx, "Employee", "employee@example.com", "Employee#123", UserRole.Employee, 1);

        var ticketId = await SeedTicketAsync(ctx, CreateTicket(
            createdById: employeeId,
            category: 1,
            priority: TicketPriority.Medium,
            status: TicketStatus.New, // Mutable for employee
            title: "Original Title",
            description: "Original Description"));

        await AuthenticateAsync(ctx, "employee@example.com", "Employee#123");

        var update = new UpdateTicketRequest
        {
            Title = "Employee Updated Title",
            Description = "Employee updated description with sufficient length for validation."
        };

        var response = await ctx.Client.PatchAsJsonAsync($"/api/tickets/{ticketId}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TicketDetailsDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.Title.Should().Be("Employee Updated Title");
        result.Description.Should().Be("Employee updated description with sufficient length for validation.");
    }

    [Fact]
    public async Task UpdateTicket_EmployeeCannotChangePriority_Returns403()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var employeeId = await SeedUserAsync(ctx, "Employee", "employee@example.com", "Employee#123", UserRole.Employee, 1);

        var ticketId = await SeedTicketAsync(ctx, CreateTicket(
            createdById: employeeId,
            category: 1,
            priority: TicketPriority.Medium,
            status: TicketStatus.New,
            title: "Test Ticket",
            description: "Test Description"));

        await AuthenticateAsync(ctx, "employee@example.com", "Employee#123");

        var update = new UpdateTicketRequest
        {
            Priority = TicketPriority.Critical // Employee cannot change priority
        };

        var response = await ctx.Client.PatchAsJsonAsync($"/api/tickets/{ticketId}", update);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await AssertProblemCodeAsync(response, ErrorCodes.ForbiddenOperation);
    }

    [Fact]
    public async Task UpdateTicket_EmployeeCannotEditOthersInProgressTicket_Returns403()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var employeeId = await SeedUserAsync(ctx, "Employee", "employee@example.com", "Employee#123", UserRole.Employee, 1);
        var otherEmployeeId = await SeedUserAsync(ctx, "Other Employee", "employee2@example.com", "Employee#123", UserRole.Employee, 1);

        var ticketId = await SeedTicketAsync(ctx, CreateTicket(
            createdById: otherEmployeeId,
            category: 1,
            priority: TicketPriority.Medium,
            status: TicketStatus.InProcess,
            title: "Test Ticket",
            description: "Test Description"));

        await AuthenticateAsync(ctx, "employee@example.com", "Employee#123");

        var update = new UpdateTicketRequest
        {
            Title = "Attempted Update"
        };

        var response = await ctx.Client.PatchAsJsonAsync($"/api/tickets/{ticketId}", update);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await AssertProblemCodeAsync(response, ErrorCodes.ForbiddenOperation);
    }

    [Fact]
    public async Task UpdateTicket_SupportCanEditAssignedTicketsInSameCategory_Returns200()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var supportId = await SeedUserAsync(ctx, "Support", "support@example.com", "Support#123", UserRole.Support, 1); // IT category
        var employeeId = await SeedUserAsync(ctx, "Employee", "employee@example.com", "Employee#123", UserRole.Employee, 1);

        var ticketId = await SeedTicketAsync(ctx, CreateTicket(
            createdById: employeeId,
            category: 1, // IT
            priority: TicketPriority.Medium,
            status: TicketStatus.InProcess,
            title: "Original Title",
            description: "Original Description",
            assignedToId: supportId)); // Assigned to Support user

        await AuthenticateAsync(ctx, "support@example.com", "Support#123");

        var update = new UpdateTicketRequest
        {
            Title = "Support Updated Title",
            Description = "Support updated description with sufficient length for validation.",
            // Keep status unchanged to validate edit permissions without relying on a specific transition.
            Status = TicketStatus.InProcess
        };

        var response = await ctx.Client.PatchAsJsonAsync($"/api/tickets/{ticketId}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TicketDetailsDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.Title.Should().Be("Support Updated Title");
        result.Status.Should().Be((int)TicketStatus.InProcess);
    }

    [Fact]
    public async Task UpdateTicket_SupportCannotEditUnassignedTicket_Returns403()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var supportId = await SeedUserAsync(ctx, "Support", "support@example.com", "Support#123", UserRole.Support, 1); // IT category
        var employeeId = await SeedUserAsync(ctx, "Employee", "employee@example.com", "Employee#123", UserRole.Employee, 1);

        var ticketId = await SeedTicketAsync(ctx, CreateTicket(
            createdById: employeeId,
            category: 2, // Different category from Support
            priority: TicketPriority.Medium,
            status: TicketStatus.Open,
            title: "Unassigned Ticket",
            description: "This ticket is not assigned to anyone.",
            assignedToId: null)); // Not assigned

        await AuthenticateAsync(ctx, "support@example.com", "Support#123");

        var update = new UpdateTicketRequest
        {
            Title = "Attempted Update"
        };

        var response = await ctx.Client.PatchAsJsonAsync($"/api/tickets/{ticketId}", update);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await AssertProblemCodeAsync(response, ErrorCodes.ForbiddenOperation);
    }

    [Fact]
    public async Task UpdateTicket_TeamLeaderCanEditTicketsInOwnCategory_Returns200()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var leaderId = await SeedUserAsync(ctx, "Team Leader", "leader@example.com", "Leader#123", UserRole.TeamLeader, 1); // IT category
        var employeeId = await SeedUserAsync(ctx, "Employee", "employee@example.com", "Employee#123", UserRole.Employee, 1);

        var ticketId = await SeedTicketAsync(ctx, CreateTicket(
            createdById: employeeId,
            category: 1, // IT (same as TeamLeader)
            priority: TicketPriority.Medium,
            status: TicketStatus.Open,
            title: "Original Title",
            description: "Original Description"));

        await AuthenticateAsync(ctx, "leader@example.com", "Leader#123");

        var update = new UpdateTicketRequest
        {
            Title = "TeamLeader Updated Title",
            Priority = TicketPriority.Critical,
            Status = TicketStatus.Open,
            AssignedToUserId = leaderId
        };

        var response = await ctx.Client.PatchAsJsonAsync($"/api/tickets/{ticketId}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TicketDetailsDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.Title.Should().Be("TeamLeader Updated Title");
        result.Priority.Should().Be((int)TicketPriority.Critical);
        result.AssignedToId.Should().Be(leaderId);
    }

    [Fact]
    public async Task UpdateTicket_TeamLeaderCannotEditTicketsInDifferentCategory_Returns403()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var leaderId = await SeedUserAsync(ctx, "Team Leader", "leader@example.com", "Leader#123", UserRole.TeamLeader, 1); // IT category
        var employeeId = await SeedUserAsync(ctx, "Employee", "employee@example.com", "Employee#123", UserRole.Employee, 2); // Logistics

        var ticketId = await SeedTicketAsync(ctx, CreateTicket(
            createdById: employeeId,
            category: 2, // Logistics (different from TeamLeader's IT)
            priority: TicketPriority.Medium,
            status: TicketStatus.Open,
            title: "Logistics Ticket",
            description: "This ticket is in a different category."));

        await AuthenticateAsync(ctx, "leader@example.com", "Leader#123");

        var update = new UpdateTicketRequest
        {
            Title = "Attempted Update"
        };

        var response = await ctx.Client.PatchAsJsonAsync($"/api/tickets/{ticketId}", update);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await AssertProblemCodeAsync(response, ErrorCodes.ForbiddenOperation);
    }

    [Fact]
    public async Task UpdateTicket_NoOneCanEditResolvedTicket_Returns403()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var adminId = await SeedUserAsync(ctx, "Admin", "admin@example.com", "Admin#123", UserRole.Admin);
        var employeeId = await SeedUserAsync(ctx, "Employee", "employee@example.com", "Employee#123", UserRole.Employee, 1);

        var ticketId = await SeedTicketAsync(ctx, CreateTicket(
            createdById: employeeId,
            category: 1,
            priority: TicketPriority.Medium,
            status: TicketStatus.Resolved, // Terminal status
            title: "Resolved Ticket",
            description: "This ticket is resolved and should not be editable."));

        await AuthenticateAsync(ctx, "admin@example.com", "Admin#123");

        var update = new UpdateTicketRequest
        {
            Title = "Attempted Update"
        };

        var response = await ctx.Client.PatchAsJsonAsync($"/api/tickets/{ticketId}", update);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await AssertProblemCodeAsync(response, ErrorCodes.ForbiddenOperation);
    }

    [Fact]
    public async Task GetTicket_ReturnsCapabilityFlags_BasedOnUserRole()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var employeeId = await SeedUserAsync(ctx, "Employee", "employee@example.com", "Employee#123", UserRole.Employee, 1);

        var ticketId = await SeedTicketAsync(ctx, CreateTicket(
            createdById: employeeId,
            category: 1,
            priority: TicketPriority.Medium,
            status: TicketStatus.New, // Mutable for employee
            title: "Test Ticket",
            description: "Test Description"));

        await AuthenticateAsync(ctx, "employee@example.com", "Employee#123");

        var response = await ctx.Client.GetAsync($"/api/tickets/{ticketId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var details = await response.Content.ReadFromJsonAsync<TicketDetailsDto>(JsonOptions);
        details.Should().NotBeNull();
        details!.Capabilities.Should().NotBeNull();
        details.Capabilities!.CanEditTitle.Should().BeTrue();
        details.Capabilities.CanEditDescription.Should().BeTrue();
        details.Capabilities.CanEditCategory.Should().BeFalse();
        details.Capabilities.CanEditPriority.Should().BeFalse();
        details.Capabilities.CanEditStatus.Should().BeFalse();
        details.Capabilities.CanEditAssignment.Should().BeFalse();
    }

    #endregion

    [Fact]
    public async Task GetTicketById_ExistingTicket_Returns200AndDetails()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var creatorId = await SeedUserAsync(ctx, "Creator", "creator@example.com", "Creator#123", UserRole.Employee, 1); // IT category
        var assigneeId = await SeedUserAsync(ctx, "Support", "support@example.com", "Support#123", UserRole.Support, 1); // IT category

        var ticketId = await SeedTicketAsync(ctx, CreateTicket(
            createdById: creatorId,
            category: 1, // IT
            priority: TicketPriority.Medium,
            status: TicketStatus.Open,
            title: "Ticket for retrieval",
            description: "Ticket created to verify retrieval endpoint returns the details.",
            assignedToId: assigneeId));

        await AuthenticateAsync(ctx, "creator@example.com", "Creator#123");

        var response = await ctx.Client.GetAsync($"/api/tickets/{ticketId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var details = await response.Content.ReadFromJsonAsync<TicketDetailsDto>(JsonOptions);
        details.Should().NotBeNull();
        details!.TicketId.Should().Be(ticketId);
        details.CreatedById.Should().Be(creatorId);
        details.AssignedToId.Should().Be(assigneeId);
        details.Title.Should().Be("Ticket for retrieval");
    }

    [Fact]
    public async Task GetTickets_WithPagination_ReturnsPagedResults()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var creatorId = await SeedUserAsync(ctx, "Creator", "creator@example.com", "Creator#123", UserRole.Employee, 1); // IT category

        await AuthenticateAsync(ctx, "creator@example.com", "Creator#123");

        var baseTime = DateTime.UtcNow.AddDays(-1);
        for (var i = 0; i < 25; i++)
        {
            await SeedTicketAsync(ctx, CreateTicket(
                createdById: creatorId,
                category: 1, // IT
                priority: TicketPriority.Medium,
                status: TicketStatus.Open,
                title: $"Ticket {i + 1}",
                description: "Ticket created to validate pagination across result sets.",
                createdAt: baseTime.AddMinutes(i)));
        }

        var response = await ctx.Client.GetAsync("/api/tickets?page=2&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<TicketListItemDto>>(JsonOptions);
        page.Should().NotBeNull();
        page!.Page.Should().Be(2);
        page.Size.Should().Be(10);
        page.Total.Should().Be(25);
        page.Items.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetTickets_WithFilters_ReturnsFilteredResults()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var creatorId = await SeedUserAsync(ctx, "Creator", "creator@example.com", "Creator#123", UserRole.Employee, 1); // IT category

        await AuthenticateAsync(ctx, "creator@example.com", "Creator#123");

        var matchingTicketId = await SeedTicketAsync(ctx, CreateTicket(
            createdById: creatorId,
            category: 1, // IT
            priority: TicketPriority.High,
            status: TicketStatus.New,
            title: "Matching IT ticket",
            description: "Ticket that should be returned by the status and category filters."));

        await SeedTicketAsync(ctx, CreateTicket(
            createdById: creatorId,
            category: 1, // IT
            priority: TicketPriority.Medium,
            status: TicketStatus.Open,
            title: "Different status ticket",
            description: "This ticket differs only by status and should be filtered out."));

        await SeedTicketAsync(ctx, CreateTicket(
            createdById: creatorId,
            category: 2, // Logistics
            priority: TicketPriority.Medium,
            status: TicketStatus.New,
            title: "Different category ticket",
            description: "This ticket differs by category and should be filtered out."));

        var response = await ctx.Client.GetAsync($"/api/tickets?status={(byte)TicketStatus.New}&category={(byte)TicketCategory.IT}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<TicketListItemDto>>(JsonOptions);
        page.Should().NotBeNull();
        page!.Total.Should().Be(1);
        page.Items.Should().ContainSingle();
        var item = page.Items.Single();
        item.TicketId.Should().Be(matchingTicketId);
        item.Status.Should().Be((byte)TicketStatus.New);
        item.CategoryId.Should().Be((byte)1); // IT category
    }

    [Fact]
    public async Task ExportTickets_ReturnsCsv()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var creatorId = await SeedUserAsync(ctx, "Creator", "creator@example.com", "Creator#123", UserRole.Employee, 1); // IT category

        await AuthenticateAsync(ctx, "creator@example.com", "Creator#123");

        var matchingTicketId = await SeedTicketAsync(ctx, CreateTicket(
            createdById: creatorId,
            category: 1, // IT
            priority: TicketPriority.Medium,
            status: TicketStatus.Open,
            title: "IT Ticket Export",
            description: "Ticket included in CSV export for IT category verification."));

        await SeedTicketAsync(ctx, CreateTicket(
            createdById: creatorId,
            category: 2, // Logistics
            priority: TicketPriority.Low,
            status: TicketStatus.Open,
            title: "Logistics ticket",
            description: "Ticket excluded from export because of different category."));

        var response = await ctx.Client.GetAsync($"/api/tickets/export?category={(byte)TicketCategory.IT}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
        response.Content.Headers.ContentType?.CharSet.Should().Be("utf-8");

        var csv = await response.Content.ReadAsStringAsync();
        csv.Should().Contain("ticketId,title,category,priority,status,createdAt,updatedAt,createdByName,assignedToName");
        csv.Should().Contain("IT Ticket Export");
        csv.Should().NotContain("Logistics ticket");
        csv.Should().Contain(matchingTicketId.ToString());
    }

    private static MultipartFormDataContent BuildTicketForm(string title, string description, int category, TicketPriority priority)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(title), nameof(CreateTicketRequest.Title));
        form.Add(new StringContent(description), nameof(CreateTicketRequest.Description));
        form.Add(new StringContent(category.ToString()), nameof(CreateTicketRequest.CategoryId));
        form.Add(new StringContent(((byte)priority).ToString()), nameof(CreateTicketRequest.Priority));
        return form;
    }

    private static async Task<int> SeedUserAsync(
        IntegrationTestContext ctx,
        string name,
        string email,
        string password,
        UserRole role,
        int? category = null)
    {
        return await ctx.WithDbContextAsync(async db =>
        {
            var user = new User
            {
                Name = name,
                Email = email,
                PasswordHash = PasswordHasher.Hash(password),
                RoleId = role,
                CategoryId = category
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user.UserId;
        });
    }

    private static async Task<int> SeedCategoryAsync(
        IntegrationTestContext ctx,
        string namePl,
        string nameEn)
    {
        return await ctx.WithDbContextAsync(async db =>
        {
            var category = new Category
            {
                NamePl = namePl,
                NameEn = nameEn
            };

            db.Categories.Add(category);
            await db.SaveChangesAsync();
            return category.CategoryId;
        });
    }

    private static async Task AuthenticateAsync(IntegrationTestContext ctx, string email, string password)
    {
        var response = await ctx.Client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var setCookie = response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(v => v.StartsWith("auth_token=", StringComparison.Ordinal))
            : null;

        setCookie.Should().NotBeNull("authentication should return auth_token cookie");

        var token = setCookie!
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .First(part => part.StartsWith("auth_token=", StringComparison.Ordinal))
            .Substring("auth_token=".Length);

        ctx.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<int> SeedTicketAsync(IntegrationTestContext ctx, Ticket ticket)
    {
        await ctx.WithDbContextAsync(async db =>
        {
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
        });

        return ticket.TicketId;
    }

    private static Ticket CreateTicket(
        int createdById,
        int category,
        TicketPriority priority,
        TicketStatus status,
        string title,
        string description,
        int? assignedToId = null,
        DateTime? createdAt = null)
    {
        var timestamp = createdAt ?? DateTime.UtcNow;
        return new Ticket
        {
            Title = title,
            Description = description,
            CategoryId = category,
            Priority = priority,
            Status = status,
            CreatedById = createdById,
            AssignedToId = assignedToId,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };
    }

    private static async Task AssertProblemCodeAsync(HttpResponseMessage response, string expectedCode)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("code", out var codeProp).Should().BeTrue();
        codeProp.GetString().Should().Be(expectedCode);
    }

    private static async Task AssertValidationFieldErrorAsync(
        HttpResponseMessage response,
        string expectedTopLevelCode,
        string fieldName,
        string expectedFieldErrorCode)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("code", out var codeProp).Should().BeTrue();
        codeProp.GetString().Should().Be(expectedTopLevelCode);

        doc.RootElement.TryGetProperty("errors", out var errorsProp).Should().BeTrue();
        errorsProp.TryGetProperty(fieldName, out var fieldErrors).Should().BeTrue();
        fieldErrors.ValueKind.Should().Be(JsonValueKind.Array);

        var codes = fieldErrors.EnumerateArray()
            .Select(x => x.GetString())
            .Where(x => x is not null)
            .ToList();

        codes.Should().Contain(expectedFieldErrorCode);
    }

    #region GetAssignableUsers Tests (T1.3)

    [Fact]
    public async Task GetAssignableUsers_WithValidTicket_ReturnsFilteredUsers()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        // Seed categories
        var itCategoryId = await SeedCategoryAsync(ctx, "IT", "IT");
        var logisticsCategoryId = await SeedCategoryAsync(ctx, "Logistics", "Logistics");

        // Seed users
        var itSupport = await SeedUserAsync(ctx, "IT Support", "itsupport@test.local", "Pass#123", UserRole.Support, itCategoryId);
        var itTeamLeader = await SeedUserAsync(ctx, "IT Leader", "itleader@test.local", "Pass#123", UserRole.TeamLeader, itCategoryId);
        var logisticsSupport = await SeedUserAsync(ctx, "Logistics Support", "logsupport@test.local", "Pass#123", UserRole.Support, logisticsCategoryId);
        var admin = await SeedUserAsync(ctx, "Admin User", "admin@test.local", "Pass#123", UserRole.Admin, null);
        var employee = await SeedUserAsync(ctx, "Employee User", "employee@test.local", "Pass#123", UserRole.Employee, itCategoryId);

        // Create ticket in IT category
        int ticketId = 0;
        await ctx.WithDbContextAsync(async db =>
        {
            var ticket = new Ticket
            {
                Title = "Test Ticket for Assignment",
                Description = "This is a test ticket to verify assignable users endpoint",
                CategoryId = itCategoryId,
                Priority = TicketPriority.Medium,
                Status = TicketStatus.New,
                CreatedById = employee,
                CreatedAt = DateTime.UtcNow
            };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
            ticketId = ticket.TicketId;
        });

        // Authenticate as admin
        await AuthenticateAsync(ctx, "admin@test.local", "Pass#123");

        // Act
        var response = await ctx.Client.GetAsync($"/api/tickets/{ticketId}/assignable-users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<List<AssignableUserDto>>(JsonOptions);
        users.Should().NotBeNull();
        users!.Should().HaveCountGreaterOrEqualTo(3, "should include IT Support, IT Leader, and Admin");

        // IT Support should be included
        users.Should().Contain(u => u.UserId == itSupport, "IT Support is in same category");

        // IT TeamLeader should be included
        users.Should().Contain(u => u.UserId == itTeamLeader, "IT TeamLeader is in same category");

        // Admin should be included (exempt from category filter)
        users.Should().Contain(u => u.UserId == admin, "Admin should be included regardless of category");

        // Logistics Support should NOT be included (different category)
        users.Should().NotContain(u => u.UserId == logisticsSupport, "Logistics Support should be excluded");

        // Employee should NOT be included (wrong role)
        users.Should().NotContain(u => u.UserId == employee, "Employee should be excluded");
    }

    [Fact]
    public async Task UpdateTicket_WithValidAssignment_UpdatesAssignee()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        // Seed category and users
        var itCategoryId = await SeedCategoryAsync(ctx, "IT", "IT");
        var supportUser = await SeedUserAsync(ctx, "Support User", "support@test.local", "Pass#123", UserRole.Support, itCategoryId);
        var teamLeader = await SeedUserAsync(ctx, "Team Leader", "leader@test.local", "Pass#123", UserRole.TeamLeader, itCategoryId);
        var employee = await SeedUserAsync(ctx, "Employee User", "employee@test.local", "Pass#123", UserRole.Employee, itCategoryId);

        // Create ticket assigned to support user
        int ticketId = 0;
        await ctx.WithDbContextAsync(async db =>
        {
            var ticket = new Ticket
            {
                Title = "Test Ticket for Reassignment",
                Description = "This ticket will be reassigned to test assignment validation",
                CategoryId = itCategoryId,
                Priority = TicketPriority.Medium,
                Status = TicketStatus.New,
                CreatedById = employee,
                AssignedToId = supportUser,
                CreatedAt = DateTime.UtcNow
            };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
            ticketId = ticket.TicketId;
        });

        // Authenticate as team leader
        await AuthenticateAsync(ctx, "leader@test.local", "Pass#123");

        // Act - Reassign from supportUser to teamLeader
        var request = new UpdateTicketRequest
        {
            AssignedToUserId = teamLeader
        };
        var response = await ctx.Client.PatchAsJsonAsync($"/api/tickets/{ticketId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<TicketDetailsDto>(JsonOptions);
        updated.Should().NotBeNull();
        updated!.AssignedToId.Should().Be(teamLeader, "ticket should be reassigned to team leader");

        // Verify in database
        await ctx.WithDbContextAsync(async db =>
        {
            var ticket = await db.Tickets.FindAsync(ticketId);
            ticket.Should().NotBeNull();
            ticket!.AssignedToId.Should().Be(teamLeader);
        });
    }

    [Fact]
    public async Task UpdateTicket_WithInvalidAssigneeRole_Returns400()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        // Seed category and users
        var itCategoryId = await SeedCategoryAsync(ctx, "IT", "IT");
        var supportUser = await SeedUserAsync(ctx, "Support User", "support@test.local", "Pass#123", UserRole.Support, itCategoryId);
        var employee = await SeedUserAsync(ctx, "Employee User", "employee@test.local", "Pass#123", UserRole.Employee, itCategoryId);
        var admin = await SeedUserAsync(ctx, "Admin User", "admin@test.local", "Pass#123", UserRole.Admin, null);

        // Create ticket
        int ticketId = 0;
        await ctx.WithDbContextAsync(async db =>
        {
            var ticket = new Ticket
            {
                Title = "Test Ticket for Invalid Assignment",
                Description = "This ticket will test invalid role assignment",
                CategoryId = itCategoryId,
                Priority = TicketPriority.Medium,
                Status = TicketStatus.New,
                CreatedById = employee,
                AssignedToId = supportUser,
                CreatedAt = DateTime.UtcNow
            };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
            ticketId = ticket.TicketId;
        });

        // Authenticate as admin
        await AuthenticateAsync(ctx, "admin@test.local", "Pass#123");

        // Act - Try to assign to employee (invalid role)
        var request = new UpdateTicketRequest
        {
            AssignedToUserId = employee
        };
        var response = await ctx.Client.PatchAsJsonAsync($"/api/tickets/{ticketId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertProblemCodeAsync(response, ErrorCodes.InvalidAssigneeRole);
    }

    [Fact]
    public async Task UpdateTicket_WithCategoryMismatch_Returns400()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        // Seed categories and users
        var itCategoryId = await SeedCategoryAsync(ctx, "IT", "IT");
        var logisticsCategoryId = await SeedCategoryAsync(ctx, "Logistics", "Logistics");
        
        var itSupport = await SeedUserAsync(ctx, "IT Support", "itsupport@test.local", "Pass#123", UserRole.Support, itCategoryId);
        var logisticsSupport = await SeedUserAsync(ctx, "Logistics Support", "logsupport@test.local", "Pass#123", UserRole.Support, logisticsCategoryId);
        var employee = await SeedUserAsync(ctx, "Employee User", "employee@test.local", "Pass#123", UserRole.Employee, itCategoryId);
        var admin = await SeedUserAsync(ctx, "Admin User", "admin@test.local", "Pass#123", UserRole.Admin, null);

        // Create ticket in IT category
        int ticketId = 0;
        await ctx.WithDbContextAsync(async db =>
        {
            var ticket = new Ticket
            {
                Title = "Test Ticket for Category Mismatch",
                Description = "This ticket will test category mismatch validation",
                CategoryId = itCategoryId,
                Priority = TicketPriority.Medium,
                Status = TicketStatus.New,
                CreatedById = employee,
                AssignedToId = itSupport,
                CreatedAt = DateTime.UtcNow
            };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
            ticketId = ticket.TicketId;
        });

        // Authenticate as admin
        await AuthenticateAsync(ctx, "admin@test.local", "Pass#123");

        // Act - Try to assign to Logistics Support (category mismatch)
        var request = new UpdateTicketRequest
        {
            AssignedToUserId = logisticsSupport
        };
        var response = await ctx.Client.PatchAsJsonAsync($"/api/tickets/{ticketId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertProblemCodeAsync(response, ErrorCodes.AssigneeCategoryMismatch);
    }

    #endregion

    #region Allowed Statuses Tests

    [Fact]
    public async Task GetAllowedStatuses_ForNewTicket_ReturnsValidTransitions()
    {
        // Arrange
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        const string email = "employee@example.com";
        const string password = "Employee#123";

        var employeeId = await SeedUserAsync(ctx, "Employee", email, password, UserRole.Employee, 1);
        await AuthenticateAsync(ctx, email, password);

        // Create a ticket in New status
        using var form = BuildTicketForm(
            "Test Ticket",
            "Description long enough for validation requirements.",
            1,
            TicketPriority.Medium);
        var createResponse = await ctx.Client.PostAsync("/api/tickets", form);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CreateTicketResponse>(JsonOptions);
        var ticketId = created!.TicketId;

        // Act
        var response = await ctx.Client.GetAsync($"/api/tickets/{ticketId}/allowed-statuses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AllowedStatusesDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.AllowedStatuses.Should().HaveCount(3);
        result.AllowedStatuses.Should().Contain((byte)TicketStatus.New); // Current status always included
        result.AllowedStatuses.Should().Contain((byte)TicketStatus.Open);
        result.AllowedStatuses.Should().Contain((byte)TicketStatus.Cancelled);
    }

    [Fact]
    public async Task GetAllowedStatuses_ForResolvedTicket_ReturnsOnlyResolved()
    {
        // Arrange
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        const string email = "leader@example.com";
        const string password = "Leader#123";

        var leaderId = await SeedUserAsync(ctx, "Team Lead", email, password, UserRole.TeamLeader, 1);
        await AuthenticateAsync(ctx, email, password);

        // Create and resolve a ticket
        int ticketId = 0;
        await ctx.WithDbContextAsync(async db =>
        {
            var ticket = new Ticket
            {
                Title = "Resolved Ticket",
                Description = "This ticket is resolved",
                CategoryId = 1,
                Priority = TicketPriority.Medium,
                Status = TicketStatus.Resolved,
                CreatedById = leaderId,
                AssignedToId = leaderId,
                CreatedAt = DateTime.UtcNow
            };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
            ticketId = ticket.TicketId;
        });

        // Act
        var response = await ctx.Client.GetAsync($"/api/tickets/{ticketId}/allowed-statuses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AllowedStatusesDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.AllowedStatuses.Should().HaveCount(1); // Terminal state, only same status allowed
        result.AllowedStatuses.Should().Contain((byte)TicketStatus.Resolved);
    }

    [Fact]
    public async Task GetAllowedStatuses_WhenTicketNotFound_Returns404()
    {
        // Arrange
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        const string email = "employee@example.com";
        const string password = "Employee#123";

        await SeedUserAsync(ctx, "Employee", email, password, UserRole.Employee, 1);
        await AuthenticateAsync(ctx, email, password);

        // Act
        var response = await ctx.Client.GetAsync("/api/tickets/99999/allowed-statuses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllowedStatuses_AsUnauthorized_Returns401()
    {
        // Arrange
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var employeeId = await SeedUserAsync(ctx, "Employee", "employee@example.com", "Employee#123", UserRole.Employee, 1);

        // Create a ticket
        int ticketId = 0;
        await ctx.WithDbContextAsync(async db =>
        {
            var ticket = new Ticket
            {
                Title = "Test Ticket",
                Description = "Description",
                CategoryId = 1,
                Priority = TicketPriority.Medium,
                Status = TicketStatus.New,
                CreatedById = employeeId,
                CreatedAt = DateTime.UtcNow
            };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
            ticketId = ticket.TicketId;
        });

        // Act - No authentication
        var response = await ctx.Client.GetAsync($"/api/tickets/{ticketId}/allowed-statuses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    private sealed class PagedResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int Size { get; set; }
    }

    private sealed class AssignableUserDto
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string? CategoryNamePl { get; set; }
        public string? CategoryNameEn { get; set; }
    }
}
