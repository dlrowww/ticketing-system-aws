using Microsoft.Extensions.Options;

using System.Net;
using System.Security.Claims;

using TicketingSystem.Api.Common;
using TicketingSystem.Api.Utils;

namespace TicketingSystem.Api.Services
{
    /// <summary>
    /// Resolves the current user's numeric ID from HttpContext.
    /// Falls back to a configured DevUserId for local/dev scenarios.
    /// </summary>
    public sealed class CurrentUserService : ICurrentUserService
    {
        private const string CacheKey = "__CurrentUserId";

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly CurrentUserOptions _options;
        private readonly IHostEnvironment _env;

        public CurrentUserService(
            IHttpContextAccessor httpContextAccessor,
            IOptions<CurrentUserOptions> options,
            IHostEnvironment env)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _options = options?.Value ?? new CurrentUserOptions();
            _env = env;
        }

        public int GetUserId()
        {
            var id = TryGetUserId();
            if (id is int v) return v;

            // No auth => allow dev fallback only when in Development
            if (_env.IsDevelopment())
            {
                if (_options.DevUserId <= 0)
                    throw new InvalidOperationException("CurrentUserOptions.DevUserId must be > 0 in development fallback.");
                return _options.DevUserId;
            }

            throw new AppException(
                ErrorCodes.Unauthenticated,
                "Authentication required.",
                HttpStatusCode.Unauthorized);
        }

        public int? TryGetUserId()
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx is null) return null;

            // per-request cache
            if (ctx.Items.TryGetValue(CacheKey, out var cached) && cached is int cachedId)
                return cachedId;

            var user = ctx.User;
            var isAuth = user?.Identity?.IsAuthenticated ?? false;
            if (!isAuth) return null;

            // Prefer explicit numeric id claims in this order
            var id = ParseFirstIntClaim(user,
                ClaimTypes.NameIdentifier,  // standard
                "uid", "user_id", "id"      // common custom
            );

            // If only "sub" exists and it's NUMERIC, accept it; if it's GUID, reject (don’t silently fallback)
            id ??= ParseFirstIntClaim(user, "sub");

            if (id is null)
            {
                // Authenticated but no numeric id => config/identity mapping issue
                throw new AppException(ErrorCodes.IdentityMappingMissing,
                    "Authenticated user has no numeric identifier claim.", HttpStatusCode.Unauthorized);
            }

            ctx.Items[CacheKey] = id.Value;
            return id.Value;
        }

        private static int? ParseFirstIntClaim(ClaimsPrincipal user, params string[] claimTypes)
        {
            foreach (var type in claimTypes)
            {
                var val = user.FindFirst(type)?.Value;
                if (!string.IsNullOrWhiteSpace(val) && int.TryParse(val, out var parsed))
                    return parsed;
            }
            return null;
        }
    }
}