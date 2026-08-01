using Microsoft.AspNetCore.Mvc;

using System.Text;

using TicketingSystem.Api.DTOs.Lookups;
using TicketingSystem.Api.Enums.History;
using TicketingSystem.Api.Enums.Identity;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.Services.Localization;
using TicketingSystem.Api.Services.Categories;

namespace TicketingSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LookupsController : ControllerBase
{
    private readonly ILocalizationService _localization;
    private readonly ICategoryService _categoryService;

    public LookupsController(ILocalizationService localization, ICategoryService categoryService)
    {
        _localization = localization;
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct = default)
    {
        // Fetch active categories from database
        var categories = await _categoryService.GetAllAsync(includeInactive: false, ct);
        var categoryLookups = categories
            .Select(c => new LookupItem(c.CategoryId, $"category_{ToKey(c.NameEn)}"))
            .OrderBy(x => x.Id)
            .ToList();

        var response = new LookupsResponse(
            MapEnum<TicketStatus>("ticket_status_"),
            MapEnum<TicketPriority>("priority_"),
            categoryLookups,
            MapEnum<UserRole>("role_"),
            MapEnum<HistoryChangeType>("history_change_type_"),
            await VersionStampAsync(categories, ct)
        );

        // Optional: ETag support for caching
        var etag = $"W/\"{response.Version}\"";
        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "public, max-age=3600"; // 1 hour (reduced from 24h since categories are dynamic)

        var inm = Request.Headers.IfNoneMatch.ToString();
        if (!string.IsNullOrWhiteSpace(inm) && inm == etag)
            return StatusCode(304);

        return Ok(response);
    }

    private static IReadOnlyList<LookupItem> MapEnum<T>(string prefix) where T : Enum
    {
        return Enum.GetValues(typeof(T))
            .Cast<T>()
            .Select(v => new LookupItem(Convert.ToInt32(v), prefix + ToKey(v.ToString())))
            .OrderBy(x => x.Id)
            .ToList();
    }

    private static string ToKey(string name)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsUpper(c) && i > 0 && char.IsLower(name[i - 1])) sb.Append('_');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    private static string VersionStamp()
    {
        var payload = string.Join("|",
            string.Join(',', Enum.GetValues<TicketStatus>().Select(e => $"{Convert.ToInt32(e)}:{e}")),
            string.Join(',', Enum.GetValues<TicketPriority>().Select(e => $"{Convert.ToInt32(e)}:{e}")),
            string.Join(',', Enum.GetValues<UserRole>().Select(e => $"{Convert.ToInt32(e)}:{e}")),
            string.Join(',', Enum.GetValues<HistoryChangeType>().Select(e => $"{Convert.ToInt32(e)}:{e}"))
        );

        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash)[..8];
    }

    private async Task<string> VersionStampAsync(IReadOnlyList<DTOs.Categories.CategoryDto> categories, CancellationToken ct)
    {
        var payload = string.Join("|",
            string.Join(',', Enum.GetValues<TicketStatus>().Select(e => $"{Convert.ToInt32(e)}:{e}")),
            string.Join(',', Enum.GetValues<TicketPriority>().Select(e => $"{Convert.ToInt32(e)}:{e}")),
            string.Join(',', categories.Select(c => $"{c.CategoryId}:{c.NameEn}")),
            string.Join(',', Enum.GetValues<UserRole>().Select(e => $"{Convert.ToInt32(e)}:{e}")),
            string.Join(',', Enum.GetValues<HistoryChangeType>().Select(e => $"{Convert.ToInt32(e)}:{e}"))
        );

        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash)[..8];
    }

    /// <summary>
    /// Gets all translations for a specific locale (for frontend synchronization).
    /// </summary>
    /// <param name="locale">Locale code ("pl" or "en")</param>
    /// <returns>Dictionary containing all translations for the specified locale</returns>
    [HttpGet("translations/{locale}")]
    public IActionResult GetTranslations([FromRoute] string locale)
    {
        if (locale != "pl" && locale != "en")
        {
            return BadRequest(new { error = "Invalid locale. Must be 'pl' or 'en'." });
        }

        var translations = _localization.GetAllTranslations(locale);
        
        if (translations == null || !translations.Any())
        {
            return NotFound(new { error = $"Translations not found for locale '{locale}'." });
        }

        return Ok(translations);
    }
}