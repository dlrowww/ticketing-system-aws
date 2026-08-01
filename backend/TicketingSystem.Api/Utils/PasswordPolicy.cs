using System.Text.RegularExpressions;

namespace TicketingSystem.Api.Utils;

/// <summary>
/// Password strength validation utility
/// Enforces minimum security requirements for user passwords
/// </summary>
public static class PasswordPolicy
{
    /// <summary>
    /// Minimum password length required
    /// </summary>
    public const int MinLength = 8;

    /// <summary>
    /// Validates password against security policy
    /// </summary>
    /// <param name="password">Password to validate</param>
    /// <returns>Validation result with error messages if invalid</returns>
    public static PasswordValidationResult Validate(string? password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password is required");
            return new PasswordValidationResult(false, errors);
        }

        if (password.Length < MinLength)
        {
            errors.Add($"Password must be at least {MinLength} characters long");
        }

        if (!Regex.IsMatch(password, "[A-Z]"))
        {
            errors.Add("Password must contain at least one uppercase letter");
        }

        if (!Regex.IsMatch(password, "[a-z]"))
        {
            errors.Add("Password must contain at least one lowercase letter");
        }

        if (!Regex.IsMatch(password, "[0-9]"))
        {
            errors.Add("Password must contain at least one digit");
        }

        if (!Regex.IsMatch(password, "[^a-zA-Z0-9]"))
        {
            errors.Add("Password must contain at least one special character (!@#$%^&* etc.)");
        }

        return new PasswordValidationResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// Calculates password strength score (0-5)
    /// </summary>
    /// <param name="password">Password to assess</param>
    /// <returns>Strength score: 0 (very weak) to 5 (very strong)</returns>
    public static int CalculateStrength(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return 0;

        int score = 0;

        // Length bonus
        if (password.Length >= 8) score++;
        if (password.Length >= 12) score++;
        if (password.Length >= 16) score++;

        // Character variety bonus
        if (Regex.IsMatch(password, "[A-Z]")) score++;
        if (Regex.IsMatch(password, "[a-z]")) score++;
        if (Regex.IsMatch(password, "[0-9]")) score++;
        if (Regex.IsMatch(password, "[^a-zA-Z0-9]")) score++;

        // Cap at 5
        return Math.Min(score, 5);
    }

    /// <summary>
    /// Gets human-readable strength description
    /// </summary>
    /// <param name="score">Strength score from CalculateStrength</param>
    /// <returns>Strength description</returns>
    public static string GetStrengthDescription(int score)
    {
        return score switch
        {
            0 or 1 => "Very Weak",
            2 => "Weak",
            3 => "Fair",
            4 => "Strong",
            5 => "Very Strong",
            _ => "Unknown"
        };
    }
}

/// <summary>
/// Result of password validation
/// </summary>
public record PasswordValidationResult(bool IsValid, IReadOnlyList<string> Errors);
