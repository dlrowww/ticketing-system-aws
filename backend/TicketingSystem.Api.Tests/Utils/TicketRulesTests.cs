using FluentAssertions;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.Utils;
using Xunit;

namespace TicketingSystem.Api.Tests.Utils;

public class TicketRulesTests
{
    [Fact]
    public void GetAllowedStatuses_FromNew_ReturnsOpenAndCancelled()
    {
        // Act
        var allowed = TicketRules.GetAllowedStatuses(TicketStatus.New);

        // Assert
        allowed.Should().HaveCount(3);
        allowed.Should().Contain(TicketStatus.New); // Current status always included
        allowed.Should().Contain(TicketStatus.Open);
        allowed.Should().Contain(TicketStatus.Cancelled);
    }

    [Fact]
    public void GetAllowedStatuses_FromOpen_ReturnsInProcessPostponedReturnedCancelled()
    {
        // Act
        var allowed = TicketRules.GetAllowedStatuses(TicketStatus.Open);

        // Assert
        allowed.Should().HaveCount(5);
        allowed.Should().Contain(TicketStatus.Open); // Current status
        allowed.Should().Contain(TicketStatus.InProcess);
        allowed.Should().Contain(TicketStatus.Postponed);
        allowed.Should().Contain(TicketStatus.Returned);
        allowed.Should().Contain(TicketStatus.Cancelled);
    }

    [Fact]
    public void GetAllowedStatuses_FromInProcess_ReturnsResolvedPostponedReturnedCancelled()
    {
        // Act
        var allowed = TicketRules.GetAllowedStatuses(TicketStatus.InProcess);

        // Assert
        allowed.Should().HaveCount(5);
        allowed.Should().Contain(TicketStatus.InProcess); // Current status
        allowed.Should().Contain(TicketStatus.Resolved);
        allowed.Should().Contain(TicketStatus.Postponed);
        allowed.Should().Contain(TicketStatus.Returned);
        allowed.Should().Contain(TicketStatus.Cancelled);
    }

    [Fact]
    public void GetAllowedStatuses_FromPostponed_ReturnsInProcessAndCancelled()
    {
        // Act
        var allowed = TicketRules.GetAllowedStatuses(TicketStatus.Postponed);

        // Assert
        allowed.Should().HaveCount(3);
        allowed.Should().Contain(TicketStatus.Postponed); // Current status
        allowed.Should().Contain(TicketStatus.InProcess);
        allowed.Should().Contain(TicketStatus.Cancelled);
    }

    [Fact]
    public void GetAllowedStatuses_FromReturned_ReturnsOpenAndCancelled()
    {
        // Act
        var allowed = TicketRules.GetAllowedStatuses(TicketStatus.Returned);

        // Assert
        allowed.Should().HaveCount(3);
        allowed.Should().Contain(TicketStatus.Returned); // Current status
        allowed.Should().Contain(TicketStatus.Open);
        allowed.Should().Contain(TicketStatus.Cancelled);
    }

    [Fact]
    public void GetAllowedStatuses_FromResolved_ReturnsOnlyResolved()
    {
        // Act
        var allowed = TicketRules.GetAllowedStatuses(TicketStatus.Resolved);

        // Assert - Terminal state, only allow staying in same status
        allowed.Should().HaveCount(1);
        allowed.Should().Contain(TicketStatus.Resolved);
    }

    [Fact]
    public void GetAllowedStatuses_FromCancelled_ReturnsOnlyCancelled()
    {
        // Act
        var allowed = TicketRules.GetAllowedStatuses(TicketStatus.Cancelled);

        // Assert - Terminal state, only allow staying in same status
        allowed.Should().HaveCount(1);
        allowed.Should().Contain(TicketStatus.Cancelled);
    }

    [Fact]
    public void GetAllowedStatuses_AlwaysIncludesCurrentStatus()
    {
        // Arrange - Test all possible statuses
        var allStatuses = Enum.GetValues<TicketStatus>();

        // Act & Assert
        foreach (var status in allStatuses)
        {
            var allowed = TicketRules.GetAllowedStatuses(status);
            allowed.Should().Contain(status, $"Current status {status} should always be in allowed list");
        }
    }
}
