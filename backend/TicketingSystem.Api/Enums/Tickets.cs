namespace TicketingSystem.Api.Enums.Tickets;

public enum TicketStatus : byte
{
    New = 1,
    Open = 2,
    InProcess = 3,
    Resolved = 4,
    Cancelled = 5,
    Postponed = 6,
    Returned = 7
}

public enum TicketPriority : byte
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum TicketCategory : byte
{
    IT = 1,
    Logistics = 2,
    Administration = 3
}

public enum TicketEventType : byte
{
    Created = 1,
    Assigned = 2,
    StatusChanged = 3,
    AttachmentAdded = 4
}