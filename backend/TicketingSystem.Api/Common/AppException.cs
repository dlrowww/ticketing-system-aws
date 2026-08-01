#nullable enable
using System.Net;

namespace TicketingSystem.Api.Common
{
    public interface IHasErrorCode
    {
        string Code { get; }
        HttpStatusCode Status { get; }
    }

    /// <summary>Throw this from services when you want a specific code+status.</summary>
    public class AppException : Exception, IHasErrorCode
    {
        public string Code { get; }
        public HttpStatusCode Status { get; }

        public AppException(string code, string message,
            HttpStatusCode status = HttpStatusCode.BadRequest,
            Exception? inner = null) : base(message, inner)
        {
            Code = code;
            Status = status;
        }
    }
}