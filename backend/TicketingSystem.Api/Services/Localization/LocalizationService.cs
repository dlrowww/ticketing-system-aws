using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace TicketingSystem.Api.Services.Localization;

/// <summary>
/// Implementation of ILocalizationService that reads translations from external JSON files.
/// Uses in-memory caching to improve performance.
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly ILogger<LocalizationService> _logger;
    private readonly IMemoryCache _cache;
    private readonly string _localizationPath;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public LocalizationService(
        ILogger<LocalizationService> logger,
        IMemoryCache cache,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _cache = cache;
        _localizationPath = Path.Combine(environment.ContentRootPath, "Localization");
    }

    public string GetEnumTranslation(string enumType, string enumValue, string locale)
    {
        var translations = GetAllTranslations(locale);
        
        if (translations.TryGetValue(enumType, out var enumTranslations) &&
            enumTranslations.TryGetValue(enumValue, out var translation))
        {
            return translation;
        }

        _logger.LogWarning(
            "Translation not found for {EnumType}.{EnumValue} in locale {Locale}",
            enumType, enumValue, locale);
        
        return enumValue; // Fallback to enum value
    }

    public string GetBilingualEnum(string enumType, string enumValue)
    {
        var plTranslation = GetEnumTranslation(enumType, enumValue, "pl");
        var enTranslation = GetEnumTranslation(enumType, enumValue, "en");
        
        return $"{plTranslation} / {enTranslation}";
    }

    public string GetEmailLabel(string labelKey, string locale)
    {
        var translations = GetAllTranslations(locale);
        
        if (translations.TryGetValue("EmailLabels", out var emailLabels) &&
            emailLabels.TryGetValue(labelKey, out var translation))
        {
            return translation;
        }

        _logger.LogWarning(
            "Email label translation not found for {LabelKey} in locale {Locale}",
            labelKey, locale);
        
        return labelKey; // Fallback to key
    }

    public Dictionary<string, Dictionary<string, string>> GetAllTranslations(string locale)
    {
        var cacheKey = $"translations_{locale}";
        
        if (_cache.TryGetValue(cacheKey, out Dictionary<string, Dictionary<string, string>>? cached))
        {
            return cached!;
        }

        var translations = LoadTranslationsFromFile(locale);
        
        _cache.Set(cacheKey, translations, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration
        });

        return translations;
    }

    private Dictionary<string, Dictionary<string, string>> LoadTranslationsFromFile(string locale)
    {
        var filePath = Path.Combine(_localizationPath, $"translations.{locale}.json");
        
        if (!File.Exists(filePath))
        {
            _logger.LogError("Translation file not found: {FilePath}", filePath);
            return new Dictionary<string, Dictionary<string, string>>();
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var translations = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
            
            if (translations == null)
            {
                _logger.LogError("Failed to deserialize translations from {FilePath}", filePath);
                return new Dictionary<string, Dictionary<string, string>>();
            }

            _logger.LogInformation("Loaded translations for locale {Locale} from {FilePath}", locale, filePath);
            return translations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading translations from {FilePath}", filePath);
            return new Dictionary<string, Dictionary<string, string>>();
        }
    }
}
