using FluentAssertions;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.Enums.Identity;
using TicketingSystem.Api.Models;
using TicketingSystem.Api.Services.Tickets.Policies;
using Xunit;

namespace TicketingSystem.Api.Tests.Services.Tickets.Policies;

public class TicketEditPolicyTests
{
    #region Admin Role Tests

    [Fact]
    public void ComputeCapabilities_AdminRole_HasFullAccess()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            CreatedById = 100,
            CategoryId = 1,
            Status = TicketStatus.Open
        };
        var admin = new User
        {
            UserId = 200,
            RoleId = UserRole.Admin
        };

        // Act
        var caps = TicketEditPolicy.ComputeCapabilities(ticket, admin);

        // Assert
        caps.CanEdit.Should().BeTrue("admin should have edit access");
        caps.CanEditTitle.Should().BeTrue("admin should edit title");
        caps.CanEditDescription.Should().BeTrue("admin should edit description");
        caps.CanEditCategory.Should().BeTrue("admin should edit category");
        caps.CanEditPriority.Should().BeTrue("admin should edit priority");
        caps.CanEditStatus.Should().BeTrue("admin should edit status");
        caps.CanEditAssignment.Should().BeTrue("admin should edit assignment");
    }

    [Fact]
    public void ComputeCapabilities_AdminRole_TerminalStatus_NoAccess()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            CreatedById = 100,
            CategoryId = 1,
            Status = TicketStatus.Resolved
        };
        var admin = new User
        {
            UserId = 200,
            RoleId = UserRole.Admin
        };

        // Act
        var caps = TicketEditPolicy.ComputeCapabilities(ticket, admin);

        // Assert
        caps.CanEdit.Should().BeFalse("resolved tickets cannot be edited");
        caps.CanEditTitle.Should().BeFalse();
        caps.CanEditDescription.Should().BeFalse();
        caps.CanEditCategory.Should().BeFalse();
        caps.CanEditPriority.Should().BeFalse();
        caps.CanEditStatus.Should().BeFalse();
        caps.CanEditAssignment.Should().BeFalse();
    }

    [Theory]
    [InlineData(TicketStatus.Cancelled)]
    [InlineData(TicketStatus.Resolved)]
    public void ComputeCapabilities_TerminalStatuses_BlockAllEdits(TicketStatus terminalStatus)
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            CreatedById = 100,
            CategoryId = 1,
            Status = terminalStatus
        };
        var admin = new User
        {
            UserId = 200,
            RoleId = UserRole.Admin
        };

        // Act
        var caps = TicketEditPolicy.ComputeCapabilities(ticket, admin);

        // Assert
        caps.CanEdit.Should().BeFalse($"{terminalStatus} tickets cannot be edited");
    }

    #endregion

    #region TeamLeader Role Tests

    [Fact]
    public void ComputeCapabilities_TeamLeader_InCategory_HasAccess()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            CreatedById = 100,
            CategoryId = 5, // IT category
            Status = TicketStatus.Open
        };
        var teamLeader = new User
        {
            UserId = 200,
            RoleId = UserRole.TeamLeader,
            CategoryId = 5 // Same category
        };

        // Act
        var caps = TicketEditPolicy.ComputeCapabilities(ticket, teamLeader);

        // Assert
        caps.CanEdit.Should().BeTrue("team leader should edit tickets in their category");
        caps.CanEditTitle.Should().BeTrue();
        caps.CanEditDescription.Should().BeTrue();
        caps.CanEditCategory.Should().BeTrue();
        caps.CanEditPriority.Should().BeTrue();
        caps.CanEditStatus.Should().BeTrue();
        caps.CanEditAssignment.Should().BeTrue();
    }

    [Fact]
    public void ComputeCapabilities_TeamLeader_OutsideCategory_NoAccess()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            CreatedById = 100,
            CategoryId = 5, // IT category
            Status = TicketStatus.Open
        };
        var teamLeader = new User
        {
            UserId = 200,
            RoleId = UserRole.TeamLeader,
            CategoryId = 10 // Different category (Logistics)
        };

        // Act
        var caps = TicketEditPolicy.ComputeCapabilities(ticket, teamLeader);

        // Assert
        caps.CanEdit.Should().BeFalse("team leader should not edit tickets outside their category");
    }

    [Fact]
    public void ComputeCapabilities_TeamLeader_NullCategory_NoAccess()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            CreatedById = 100,
            CategoryId = 5,
            Status = TicketStatus.Open
        };
        var teamLeader = new User
        {
            UserId = 200,
            RoleId = UserRole.TeamLeader,
            CategoryId = null // No category assigned
        };

        // Act
        var caps = TicketEditPolicy.ComputeCapabilities(ticket, teamLeader);

        // Assert
        caps.CanEdit.Should().BeFalse("team leader without category should not edit");
    }

    #endregion

    #region Support Role Tests

    [Fact]
    public void ComputeCapabilities_Support_AssignedToTicket_HasAccess()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            CreatedById = 100,
            CategoryId = 5,
            AssignedToId = 200, // Assigned to this support user
            Status = TicketStatus.InProcess
        };
        var support = new User
        {
            UserId = 200,
            RoleId = UserRole.Support,
            CategoryId = 5
        };

        // Act
        var caps = TicketEditPolicy.ComputeCapabilities(ticket, support);

        // Assert
        caps.CanEdit.Should().BeTrue("support should edit tickets assigned to them");
        caps.CanEditTitle.Should().BeTrue();
        caps.CanEditDescription.Should().BeTrue();
        caps.CanEditCategory.Should().BeTrue();
        caps.CanEditPriority.Should().BeTrue();
        caps.CanEditStatus.Should().BeTrue();
        caps.CanEditAssignment.Should().BeTrue();
    }

    [Fact]
    public void ComputeCapabilities_Support_InCategory_NotAssigned_HasAccess()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            CreatedById = 100,
            CategoryId = 5, // IT category
            AssignedToId = 300, // Assigned to someone else
            Status = TicketStatus.Open
        };
        var support = new User
        {
            UserId = 200,
            RoleId = UserRole.Support,
            CategoryId = 5 // Same category
        };

        // Act
        var caps = TicketEditPolicy.ComputeCapabilities(ticket, support);

        // Assert
        caps.CanEdit.Should().BeTrue("support should edit tickets in their category");
    }

    [Fact]
    public void ComputeCapabilities_Support_OutsideCategory_NotAssigned_NoAccess()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            CreatedById = 100,
            CategoryId = 5, // IT category
            AssignedToId = 300,
            Status = TicketStatus.Open
        };
        var support = new User
        {
            UserId = 200,
            RoleId = UserRole.Support,
            CategoryId = 10 // Different category
        };

        // Act
        var caps = TicketEditPolicy.ComputeCapabilities(ticket, support);

        // Assert
        caps.CanEdit.Should().BeFalse("support should not edit tickets outside their category if not assigned");
    }

    #endregion

    #region Employee Role Tests

    [Fact]
    public void ComputeCapabilities_Employee_Creator_MutableStatus_LimitedAccess()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            CreatedById = 200, // Created by this employee
            CategoryId = 5,
            Status = TicketStatus.New
        };
        var employee = new User
        {
            UserId = 200,
            RoleId = UserRole.Employee
        };

        // Act
        var caps = TicketEditPolicy.ComputeCapabilities(ticket, employee);

        // Assert
        caps.CanEdit.Should().BeTrue("employee should edit their own tickets in mutable states");
        caps.CanEditTitle.Should().BeTrue("employee can edit title");
        caps.CanEditDescription.Should().BeTrue("employee can edit description");
        caps.CanEditCategory.Should().BeFalse("employee cannot edit category");
        caps.CanEditPriority.Should().BeFalse("employee cannot edit priority");
        caps.CanEditStatus.Should().BeFalse("employee cannot edit status");
        caps.CanEditAssignment.Should().BeFalse("employee cannot edit assignment");
    }

    [Fact]
    public void ComputeCapabilities_Employee_Creator_InProgressStatus_LimitedAccess()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            CreatedById = 200,
            CategoryId = 5,
            Status = TicketStatus.InProcess
        };
        var employee = new User
        {
            UserId = 200,
            RoleId = UserRole.Employee
        };

        // Act
        var caps = TicketEditPolicy.ComputeCapabilities(ticket, employee);

        // Assert
        caps.CanEdit.Should().BeTrue("employee should edit their own tickets in InProcess state");
        caps.CanEditTitle.Should().BeTrue();
        caps.CanEditDescription.Should().BeTrue();
        caps.CanEditCategory.Should().BeFalse();
        caps.CanEditPriority.Should().BeFalse();
        caps.CanEditStatus.Should().BeFalse();
        caps.CanEditAssignment.Should().BeFalse();
    }

    [Theory]
    [InlineData(TicketStatus.New)]
    [InlineData(TicketStatus.Open)]
    public void ComputeCapabilities_Employee_Creator_MutableStatuses_CanEdit(TicketStatus status)
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            CreatedById = 200,
            CategoryId = 5,
            Status = status
        };
        var employee = new User
        {
            UserId = 200,
            RoleId = UserRole.Employee
        };

        // Act
        var caps = TicketEditPolicy.ComputeCapabilities(ticket, employee);

        // Assert
        caps.CanEdit.Should().BeTrue($"employee should edit their own tickets in {status} state");
    }

    [Fact]
    public void ComputeCapabilities_Employee_NotCreator_NoAccess()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            CreatedById = 300, // Created by someone else
            CategoryId = 5,
            Status = TicketStatus.New
        };
        var employee = new User
        {
            UserId = 200,
            RoleId = UserRole.Employee
        };

        // Act
        var caps = TicketEditPolicy.ComputeCapabilities(ticket, employee);

        // Assert
        caps.CanEdit.Should().BeFalse("employee cannot edit tickets they didn't create");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ComputeCapabilities_UnknownRole_NoAccess()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            CreatedById = 100,
            CategoryId = 5,
            Status = TicketStatus.Open
        };
        var unknownUser = new User
        {
            UserId = 200,
            RoleId = (UserRole)99 // Invalid role
        };

        // Act
        var caps = TicketEditPolicy.ComputeCapabilities(ticket, unknownUser);

        // Assert
        caps.CanEdit.Should().BeFalse("unknown roles should not have access");
    }

    #endregion
}
