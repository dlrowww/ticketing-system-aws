using TicketingSystem.Api.DTOs.Tickets;
using TicketingSystem.Api.Enums.Identity;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.Models;

namespace TicketingSystem.Api.Services.Tickets.Policies;

/// <summary>
/// Defines edit capabilities for tickets based on user role, ticket state, and ownership.
/// </summary>
public static class TicketEditPolicy
{
    /// <summary>
    /// Computes what operations the current user can perform on the given ticket.
    /// </summary>
    public static TicketEditCapabilities ComputeCapabilities(Ticket ticket, User currentUser)
    {
        var role = currentUser.RoleId;
        var userId = currentUser.UserId;
        var userCategoryId = currentUser.CategoryId;

        // Terminal states cannot be edited
        var isTerminal = ticket.Status is TicketStatus.Resolved or TicketStatus.Cancelled;

        // Determine base edit permission
        bool canEdit = role switch
        {
            UserRole.Admin => true,
            UserRole.TeamLeader => IsInTeamLeaderScope(ticket, userCategoryId),
            UserRole.Support => IsAssignedOrInCategory(ticket, userId, userCategoryId),
            UserRole.Employee => IsCreatorAndMutable(ticket, userId),
            _ => false
        };

        if (isTerminal)
        {
            canEdit = false;
        }

        // Field-level permissions (all respect canEdit base permission)
        var capabilities = new TicketEditCapabilities
        {
            CanEdit = canEdit,
            CanEditTitle = canEdit,
            CanEditDescription = canEdit,
            CanEditCategory = canEdit && (role is UserRole.Admin or UserRole.TeamLeader or UserRole.Support),
            CanEditPriority = canEdit && (role is UserRole.Admin or UserRole.TeamLeader or UserRole.Support),
            CanEditStatus = canEdit && (role is UserRole.Admin or UserRole.TeamLeader or UserRole.Support),
            CanEditAssignment = canEdit && (role is UserRole.Admin or UserRole.TeamLeader or UserRole.Support)
        };

        return capabilities;
    }

    private static bool IsInTeamLeaderScope(Ticket ticket, int? userCategoryId)
    {
        return userCategoryId.HasValue && ticket.CategoryId == userCategoryId.Value;
    }

    private static bool IsAssignedOrInCategory(Ticket ticket, int userId, int? userCategoryId)
    {
        // Support can edit if assigned to them OR if ticket is in their category
        if (ticket.AssignedToId == userId) return true;
        if (userCategoryId.HasValue && ticket.CategoryId == userCategoryId.Value) return true;
        return false;
    }

    private static bool IsCreatorAndMutable(Ticket ticket, int userId)
    {
        // Employee can only edit their own tickets and only in early stages
        if (ticket.CreatedById != userId) return false;

        // Allow edits while ticket is still in an active (non-terminal) workflow state.
        // Terminal states (Resolved/Cancelled) are blocked for everyone at the policy root.
        return ticket.Status is TicketStatus.New
            or TicketStatus.Open
            or TicketStatus.InProcess
            or TicketStatus.Postponed
            or TicketStatus.Returned;
    }
}
