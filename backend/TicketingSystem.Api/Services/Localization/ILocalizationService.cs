namespace TicketingSystem.Api.Services.Localization;

/// <summary>
/// Service for retrieving translations from external JSON files.
/// Provides translations for enums, email labels, and other localized content.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets translation for a specific enum value in a single language.
    /// </summary>
    /// <param name="enumType">Type of enum (e.g., "TicketStatus", "TicketPriority")</param>
    /// <param name="enumValue">Enum value name (e.g., "New", "High")</param>
    /// <param name="locale">Locale code ("pl" or "en")</param>
    /// <returns>Translated string, or enum value if translation not found</returns>
    string GetEnumTranslation(string enumType, string enumValue, string locale);

    /// <summary>
    /// Gets bilingual translation for an enum value in format "Polish / English".
    /// </summary>
    /// <param name="enumType">Type of enum (e.g., "TicketStatus", "TicketPriority")</param>
    /// <param name="enumValue">Enum value name (e.g., "New", "High")</param>
    /// <returns>Bilingual string in format "Nowe / New"</returns>
    string GetBilingualEnum(string enumType, string enumValue);

    /// <summary>
    /// Gets translation for an email label in a single language.
    /// </summary>
    /// <param name="labelKey">Label key (e.g., "TicketId", "Hello")</param>
    /// <param name="locale">Locale code ("pl" or "en")</param>
    /// <returns>Translated label, or key if translation not found</returns>
    string GetEmailLabel(string labelKey, string locale);

    /// <summary>
    /// Gets all translations for a specific locale.
    /// Used by API endpoint for frontend synchronization.
    /// </summary>
    /// <param name="locale">Locale code ("pl" or "en")</param>
    /// <returns>Dictionary containing all translations for the locale</returns>
    Dictionary<string, Dictionary<string, string>> GetAllTranslations(string locale);
}
