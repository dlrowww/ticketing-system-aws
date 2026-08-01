namespace TicketingSystem.Api.Services
{
    public interface IAssignmentService
    {
        /// <summary>
        /// On ticket create, auto-assign ONLY to the Team Leader in the same category.
        /// If no TL exists, return null (unassigned).
        /// </summary>
        Task<int?> ResolveAssigneeAsync(int categoryId, CancellationToken ct);
    }
}