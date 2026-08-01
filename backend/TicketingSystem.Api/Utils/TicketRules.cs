using TicketingSystem.Api.Enums.Tickets;

namespace TicketingSystem.Api.Utils
{
    public static class TicketRules
    {
        /// <summary>
        /// Allowed transitions:
        /// Core flow: New → Open → InProcess → Resolved (terminal)
        /// Extras:
        ///   - Canceled: terminal (the issue becomes irrelevant - can cancel from most non-terminal states)
        ///   - Postponed: temporary (the issue cannot be addressed immediately); resume to InProcess (or cancel)
        ///   - Returned: needs clarification; only goes back to Open (or cancel)
        /// </summary>
        public static bool IsAllowedTransition(TicketStatus from, TicketStatus to)
        {
            if (from == to) return true;

            return from switch
            {
                TicketStatus.New => to is TicketStatus.Open 
                    or TicketStatus.Cancelled,

                TicketStatus.Open   => to is TicketStatus.InProcess
                    or TicketStatus.Postponed
                    or TicketStatus.Returned
                    or TicketStatus.Cancelled,

                TicketStatus.InProcess => to is TicketStatus.Resolved
                    or TicketStatus.Postponed
                    or TicketStatus.Returned
                    or TicketStatus.Cancelled,

                TicketStatus.Postponed => to is TicketStatus.InProcess
                    or TicketStatus.Cancelled,

                TicketStatus.Returned => to is TicketStatus.Open
                    or TicketStatus.Cancelled,

                TicketStatus.Resolved => false,
                TicketStatus.Cancelled => false,

                _ => false
            };
        }

        /// <summary>
        /// Returns list of all valid status transitions from the current status.
        /// Always includes the current status itself (to allow "no change").
        /// </summary>
        public static IReadOnlyList<TicketStatus> GetAllowedStatuses(TicketStatus currentStatus)
        {
            var allowed = new List<TicketStatus> { currentStatus }; // Always allow keeping same status

            foreach (var status in Enum.GetValues<TicketStatus>())
            {
                if (status != currentStatus && IsAllowedTransition(currentStatus, status))
                {
                    allowed.Add(status);
                }
            }

            return allowed.OrderBy(s => (byte)s).ToList();
        }
    }
}