#nullable enable
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net;

namespace TicketingSystem.Api.Common
{
    public static class ProblemDetailsExtensions
    {
        /// <summary>
        /// Create a ProblemDetails with a machine-readable error code for FE localization.
        /// </summary>
        public static ProblemDetails CreateProblem(
            HttpContext httpContext,
            HttpStatusCode status,
            string title,
            string? detail = null,
            string? type = null,
            string? code = null)
        {
            var pd = new ProblemDetails
            {
                Status = (int)status,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path,
                Type = type ?? $"https://httpstatuses.com/{(int)status}"
            };

            // Diagnostics & FE localization
            pd.Extensions["traceId"] = httpContext.TraceIdentifier;
            if (!string.IsNullOrWhiteSpace(code))
                pd.Extensions["code"] = code; // <-- FE reads this and maps to localized text

            return pd;
        }

        public static ValidationProblemDetails CreateValidationProblem(
            HttpContext httpContext,
            ModelStateDictionary modelState,
            string? title = null,
            string code = ErrorCodes.ValidationFailed)
        {
            var vpd = new ValidationProblemDetails(modelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = title ?? "Validation Failed",
                Instance = httpContext.Request.Path,
                Type = "https://httpstatuses.com/400"
            };

            vpd.Extensions["traceId"] = httpContext.TraceIdentifier;
            vpd.Extensions["code"] = code; // FE key for global validation error message

            return vpd;
        }

        /// <summary>
        /// Map exceptions to ProblemDetails + error code.
        /// Extend this as you add domain-specific exceptions.
        /// </summary>
        public static ProblemDetails FromException(HttpContext ctx, Exception ex, ILogger logger)
        {
            return ex switch
            {
                AppValidationException vex => CreateValidationProblem(
                    ctx,
                    ToModelState(vex.Errors),
                    title: "Validation Failed",
                    code: vex.Code
                ),

                IHasErrorCode hex => CreateProblem(
                    ctx,
                    hex.Status,
                    title: StatusToTitle(hex.Status),
                    detail: ex.Message,
                    code: hex.Code
                ),

                KeyNotFoundException kex => CreateProblem(
                    ctx, HttpStatusCode.NotFound, "Not found", kex.Message, code: ErrorCodes.NotFound),

                UnauthorizedAccessException uex => CreateProblem(
                    ctx, HttpStatusCode.Forbidden, "Access denied", uex.Message, code: ErrorCodes.AccessDenied),

                InvalidOperationException ioex => CreateProblem(
                    ctx, HttpStatusCode.Conflict, "Operation not allowed", ioex.Message, code: ErrorCodes.Conflict),

                ArgumentException aex => CreateProblem(
                    ctx, HttpStatusCode.BadRequest, "Invalid argument", aex.Message, code: ErrorCodes.ValidationFailed),

                // Fallback
                _ => CreateProblem(
                    ctx, HttpStatusCode.InternalServerError, "Unexpected error",
                    detail: "An unexpected error occurred. Contact support with the traceId.",
                    code: ErrorCodes.InternalError)
            };

            static string StatusToTitle(HttpStatusCode s) => s switch
            {
                HttpStatusCode.NotFound => "Not found",
                HttpStatusCode.Conflict => "Conflict",
                HttpStatusCode.Forbidden => "Access denied",
                HttpStatusCode.BadRequest => "Bad request",
                HttpStatusCode.ServiceUnavailable => "Service unavailable",
                _ => "Error"
            };

            static ModelStateDictionary ToModelState(IReadOnlyDictionary<string, string[]> errors)
            {
                var ms = new ModelStateDictionary();
                foreach (var (field, codes) in errors)
                {
                    if (codes is null) continue;
                    foreach (var c in codes)
                    {
                        if (!string.IsNullOrWhiteSpace(c))
                            ms.AddModelError(field, c);
                    }
                }
                return ms;
            }
        }

       public static IResult ToResult(this ProblemDetails pd)
        => Results.Problem(
            detail: pd.Detail,
            statusCode: pd.Status,
            title: pd.Title,
            type: pd.Type,
            instance: pd.Instance,
            extensions: pd.Extensions
        );

        public static IResult ToResult(this ValidationProblemDetails vpd)
        => Results.ValidationProblem(
            errors: vpd.Errors,
            detail: vpd.Detail,
            instance: vpd.Instance,
            statusCode: vpd.Status,
            title: vpd.Title,
            type: vpd.Type,
            extensions: vpd.Extensions
        );
        
        public static IActionResult ToActionResult(this ProblemDetails pd)
            => new ObjectResult(pd) { StatusCode = pd.Status };

        public static IActionResult ToActionResult(this ValidationProblemDetails vpd)
            => new ObjectResult(vpd) { StatusCode = vpd.Status };
    }
}