using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

using TicketingSystem.Api.Common;
using TicketingSystem.Api.Data;
using TicketingSystem.Api.DTOs;
using TicketingSystem.Api.Services;

namespace TicketingSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ICurrentUserService _currentUser;
        private readonly IHostEnvironment _env;

        public AuthController(AppDbContext context, IConfiguration configuration, ICurrentUserService currentUser, IHostEnvironment env)
        {
            _context = context;
            _configuration = configuration;
            _currentUser = currentUser;
            _env = env;
        }

        private bool ShouldUseSecureCookie() => _env.IsProduction() || Request.IsHttps;

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            {
                var pd = ProblemDetailsExtensions.CreateProblem(
                    HttpContext, HttpStatusCode.Unauthorized,
                    title: "Invalid credentials",
                    detail: "Email or password is missing.",
                    code: ErrorCodes.InvalidCredentials
                );
                return StatusCode(pd.Status ?? StatusCodes.Status401Unauthorized, pd);
            }

            var normalizedEmail = req.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            var passwordOk = false;
            if (user is not null)
            {
                try
                {
                    passwordOk = PasswordHasher.Verify(req.Password, user.PasswordHash);
                }
                catch
                {
                    // Treat malformed/legacy hashes as invalid credentials.
                    passwordOk = false;
                }
            }

            if (user is null || !passwordOk)
            {
                var pd = ProblemDetailsExtensions.CreateProblem(
                    HttpContext, HttpStatusCode.Unauthorized,
                    title: "Invalid credentials",
                    detail: "Email or password is incorrect.",
                    code: ErrorCodes.InvalidCredentials
                );
                return StatusCode(pd.Status ?? StatusCodes.Status401Unauthorized, pd);
            }

            if (!user.IsActive)
            {
                var pd = ProblemDetailsExtensions.CreateProblem(
                    HttpContext, HttpStatusCode.Unauthorized,
                    title: "Access denied",
                    detail: "User account is inactive.",
                    code: ErrorCodes.UserInactive
                );
                return StatusCode(pd.Status ?? StatusCodes.Status401Unauthorized, pd);
            }

            // Create token claims
            var claims = new[]
            {
                new Claim("id", user.UserId.ToString()),
                new Claim("name", user.Name),
                new Claim("email", user.Email),
                new Claim("roleId", ((byte)user.RoleId).ToString()),
                new Claim(ClaimTypes.Role, user.RoleId.ToString()),
                new Claim("categoryId", user.CategoryId.HasValue ? ((byte)user.CategoryId.Value).ToString() : "")
            };

            // Build JWT
            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // Set HTTP-only cookie
            Response.Cookies.Append("auth_token", tokenString, new CookieOptions
            {
                HttpOnly = true,
                Secure = ShouldUseSecureCookie(),
                SameSite = SameSiteMode.Strict, // or Lax
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            });

            // Return token in response body for API testing (Swagger, Postman, etc.)
            return Ok(new 
            { 
                code = "SUCCESS",
                token = tokenString,
                expiresAt = DateTime.UtcNow.AddHours(2)
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Append("auth_token", string.Empty, new CookieOptions
            {
                HttpOnly = true,
                Secure = ShouldUseSecureCookie(),
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(-1)
            });

            return Ok(new { code = "SUCCESS" });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<CurrentUserResponse>> GetCurrentUser()
        {
            var userId = _currentUser.GetUserId();

            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => new CurrentUserResponse(
                    u.UserId,
                    u.Name,
                    u.Email,
                    (byte)u.RoleId,
                    u.CategoryId == null ? (byte?)null : (byte)u.CategoryId))
                .SingleOrDefaultAsync();

            if (user is null)
            {
                var pd = ProblemDetailsExtensions.CreateProblem(
                    HttpContext,
                    HttpStatusCode.Unauthorized,
                    "User not found",
                    "Authenticated user record is missing.",
                    code: ErrorCodes.UserNotFound);

                return StatusCode(pd.Status ?? StatusCodes.Status401Unauthorized, pd);
            }

            return Ok(user);
        }
    }
}