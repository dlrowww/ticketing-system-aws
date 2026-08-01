namespace TicketingSystem.Api.Services
{
    public interface ICurrentUserService
    {
        /// <summary>
        /// Returns the current authenticated user's numeric ID.
        /// For MVP, may fall back to a configured dev/admin user id.
        /// </summary>
        int GetUserId();
    }
}