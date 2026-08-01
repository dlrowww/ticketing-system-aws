using System.Collections.Concurrent;
using System.Net;

namespace TicketingSystem.Api.Infrastructure.RateLimiting;

/// <summary>
/// Rate limiting middleware to prevent abuse and brute-force attacks.
/// Uses in-memory sliding window counter for request tracking.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    
    // Track requests per IP: IP -> (RequestTimestamps, LastCleanup)
    private static readonly ConcurrentDictionary<string, RequestTracker> _requestTrackers = new();
    
    // Cleanup old entries every 5 minutes to prevent memory leak
    private static DateTime _lastGlobalCleanup = DateTime.UtcNow;
    private static readonly TimeSpan GlobalCleanupInterval = TimeSpan.FromMinutes(5);
    
    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = GetClientIp(context);
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        
        // Perform global cleanup if needed
        PerformGlobalCleanupIfNeeded();
        
        // Determine rate limit based on endpoint
        var (limit, window, description) = GetRateLimitForPath(path);
        
        // Get or create tracker for this IP
        var tracker = _requestTrackers.GetOrAdd(clientIp, _ => new RequestTracker());
        
        // Check rate limit
        if (!tracker.TryRecordRequest(limit, window, out var retryAfterSeconds))
        {
            // Get current count from tracker for debugging
            var currentCount = tracker.Count;
            
            _logger.LogWarning(
                "Rate limit exceeded for IP {ClientIp} on {Path}. Current: {CurrentCount}/{Limit} requests in {Window}. Retry after {RetryAfter}s",
                clientIp, path, currentCount, limit, window, retryAfterSeconds);
            
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
            
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc6585#section-4",
                title = "Too Many Requests",
                status = 429,
                code = "RateLimitExceeded",
                detail = $"Rate limit exceeded. Maximum {limit} {description}. Try again in {retryAfterSeconds} seconds.",
                traceId = context.TraceIdentifier
            });
            
            return;
        }
        
        // Continue to next middleware
        await _next(context);
    }
    
    private static string GetClientIp(HttpContext context)
    {
        // Check X-Forwarded-For header (for proxy/load balancer scenarios)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }
        
        // Fallback to RemoteIpAddress
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
    
    private static (int Limit, TimeSpan Window, string Description) GetRateLimitForPath(string path)
    {
        // Check if we're in development mode
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        
        // Stricter limits for authentication endpoints (prevent brute force)
        if (path.Contains("/api/auth/login"))
        {
            // Development: 100 attempts per 5 minutes (very lenient for testing)
            // Production: 5 attempts per 15 minutes (strict security)
            return isDevelopment 
                ? (100, TimeSpan.FromMinutes(5), "login attempts per 5 minutes")
                : (5, TimeSpan.FromMinutes(15), "login attempts per 15 minutes");
        }
        
        // Moderate limits for file upload endpoints
        if (path.Contains("/api/tickets") && path.Contains("/files"))
        {
            return isDevelopment
                ? (200, TimeSpan.FromMinutes(5), "file operations per 5 minutes")
                : (20, TimeSpan.FromMinutes(5), "file operations per 5 minutes");
        }
        
        // General API rate limit
        return isDevelopment
            ? (1000, TimeSpan.FromMinutes(1), "requests per minute")
            : (100, TimeSpan.FromMinutes(1), "requests per minute");
    }
    
    private static void PerformGlobalCleanupIfNeeded()
    {
        var now = DateTime.UtcNow;
        if (now - _lastGlobalCleanup < GlobalCleanupInterval)
        {
            return;
        }
        
        _lastGlobalCleanup = now;
        
        // Remove trackers with no recent requests (older than 1 hour)
        var staleThreshold = now.AddHours(-1);
        var staleKeys = _requestTrackers
            .Where(kvp => kvp.Value.LastRequestAt < staleThreshold)
            .Select(kvp => kvp.Key)
            .ToList();
        
        foreach (var key in staleKeys)
        {
            _requestTrackers.TryRemove(key, out _);
        }
    }
    
    private class RequestTracker
    {
        private readonly object _lock = new();
        private readonly List<DateTime> _requestTimestamps = new();
        public DateTime LastRequestAt { get; private set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Gets the current number of requests in the active window (thread-safe read).
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _requestTimestamps.Count;
                }
            }
        }
        
        /// <summary>
        /// Attempts to record a request. Returns false if rate limit exceeded.
        /// </summary>
        /// <param name="limit">Maximum number of requests allowed</param>
        /// <param name="window">Time window for the limit</param>
        /// <param name="retryAfterSeconds">Seconds until oldest request expires (if rate limited)</param>
        public bool TryRecordRequest(int limit, TimeSpan window, out int retryAfterSeconds)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                var windowStart = now - window;
                
                // Remove requests outside the current window
                _requestTimestamps.RemoveAll(ts => ts < windowStart);
                
                // Check if limit exceeded
                if (_requestTimestamps.Count >= limit)
                {
                    // Calculate retry-after based on oldest request in window
                    var oldestRequest = _requestTimestamps.Min();
                    var retryAfter = (oldestRequest + window) - now;
                    retryAfterSeconds = Math.Max(1, (int)retryAfter.TotalSeconds);
                    return false;
                }
                
                // Record this request
                _requestTimestamps.Add(now);
                LastRequestAt = now;
                retryAfterSeconds = 0;
                return true;
            }
        }
    }
}
