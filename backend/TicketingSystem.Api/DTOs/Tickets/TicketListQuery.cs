using TicketingSystem.Api.Common;

namespace TicketingSystem.Api.DTOs.Tickets
{
    public abstract record TicketQueryBase
    {
        public string? Search { get; init; }
        public byte? Status { get; init; }
        public int? CategoryId { get; init; }
        public byte? Priority { get; init; }
        public DateOnly? DateFrom { get; init; }
        public DateOnly? DateTo { get; init; }

        public int? CreatedByUserId { get; init; }
        public int? AssignedToUserId { get; init; }
        public bool AssignedToIsNull { get; init; }

        // Sorting (FE camelCase)
        public string SortBy { get; init; } = "createdAt";
        public string SortDir { get; init; } = "desc";
    }

    public sealed record TicketListQuery : TicketQueryBase
    {
        public PageRequest Page { get; init; } = new();
    }

    public sealed record TicketExportQuery : TicketQueryBase
    {
        // no extra fields
    }
}