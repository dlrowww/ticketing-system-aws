#nullable enable
using System.Net;

namespace TicketingSystem.Api.Common
{
    /// <summary>
    /// Represents a validation failure with per-field error codes.
    /// These codes are meant for FE localization (no user-facing strings from BE).
    /// </summary>
    public sealed class AppValidationException : Exception, IHasErrorCode
    {
        public string Code { get; }
        public HttpStatusCode Status { get; } = HttpStatusCode.BadRequest;

        /// <summary>
        /// Field -> list of error codes (e.g. "Title" -> ["TICKET_TITLE_TOO_SHORT"]).
        /// </summary>
        public IReadOnlyDictionary<string, string[]> Errors { get; }

        public AppValidationException(
            IReadOnlyDictionary<string, string[]> errors,
            string code = ErrorCodes.ValidationFailed,
            Exception? inner = null)
            : base("Validation failed.", inner)
        {
            Errors = errors;
            Code = code;
        }
    }
}
