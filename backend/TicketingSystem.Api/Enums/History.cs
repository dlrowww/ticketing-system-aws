namespace TicketingSystem.Api.Enums.History;

/// <summary>
/// Types of changes that can be logged in ticket history.
/// These are used as keys for frontend translation.
/// </summary>
public enum HistoryChangeType : byte
{
    TicketCreated = 1,
    StatusChanged = 2,
    PriorityChanged = 3,
    CategoryChanged = 4,
    AssignmentChanged = 5,
    TitleChanged = 6,
    DescriptionChanged = 7,
    CommentAdded = 8,
    FileAdded = 9,
    FileRemoved = 10
}
