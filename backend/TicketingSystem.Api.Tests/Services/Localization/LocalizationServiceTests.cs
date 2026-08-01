using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

using TicketingSystem.Api.Services.Localization;

namespace TicketingSystem.Api.Tests.Services.Localization;

/// <summary>
/// Unit tests for LocalizationService.
/// Tests translation loading, caching, and bilingual format.
/// </summary>
public class LocalizationServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Mock<ILogger<LocalizationService>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly IMemoryCache _memoryCache;

    public LocalizationServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"LocalizationTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _mockLogger = new Mock<ILogger<LocalizationService>>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockEnvironment.Setup(x => x.ContentRootPath).Returns(_tempDir);

        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        CreateTestTranslationFiles();
    }

    private void CreateTestTranslationFiles()
    {
        var localizationDir = Path.Combine(_tempDir, "Localization");
        Directory.CreateDirectory(localizationDir);

        // Create Polish translations
        var plTranslations = new Dictionary<string, Dictionary<string, string>>
        {
            ["TicketStatus"] = new() { ["New"] = "Nowe", ["Open"] = "Otwarte", ["Resolved"] = "Rozwiązane" },
            ["TicketPriority"] = new() { ["Low"] = "Niski", ["Medium"] = "Średni", ["High"] = "Wysoki" },
            ["TicketCategory"] = new() { ["IT"] = "IT", ["Logistics"] = "Logistyka", ["Administrative"] = "Administracyjne" },
            ["EmailLabels"] = new() { ["Hello"] = "Witaj", ["TicketId"] = "Numer zgłoszenia" }
        };
        File.WriteAllText(
            Path.Combine(localizationDir, "translations.pl.json"),
            JsonSerializer.Serialize(plTranslations, new JsonSerializerOptions { WriteIndented = true }));

        // Create English translations
        var enTranslations = new Dictionary<string, Dictionary<string, string>>
        {
            ["TicketStatus"] = new() { ["New"] = "New", ["Open"] = "Open", ["Resolved"] = "Resolved" },
            ["TicketPriority"] = new() { ["Low"] = "Low", ["Medium"] = "Medium", ["High"] = "High" },
            ["TicketCategory"] = new() { ["IT"] = "IT", ["Logistics"] = "Logistics", ["Administrative"] = "Administrative" },
            ["EmailLabels"] = new() { ["Hello"] = "Hello", ["TicketId"] = "Ticket ID" }
        };
        File.WriteAllText(
            Path.Combine(localizationDir, "translations.en.json"),
            JsonSerializer.Serialize(enTranslations, new JsonSerializerOptions { WriteIndented = true }));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); }
            catch { /* Ignore cleanup errors */ }
        }

        _memoryCache?.Dispose();
    }

    #region GetEnumTranslation Tests

    [Fact]
    public void GetEnumTranslation_WithValidEnum_ReturnsTranslation()
    {
        // Arrange
        var service = new LocalizationService(_mockLogger.Object, _memoryCache, _mockEnvironment.Object);

        // Act
        var result = service.GetEnumTranslation("TicketStatus", "New", "pl");

        // Assert
        result.Should().Be("Nowe", "Polish translation for 'New' status should be 'Nowe'");
    }

    [Fact]
    public void GetEnumTranslation_WithEnglishLocale_ReturnsEnglishTranslation()
    {
        // Arrange
        var service = new LocalizationService(_mockLogger.Object, _memoryCache, _mockEnvironment.Object);

        // Act
        var result = service.GetEnumTranslation("TicketStatus", "Open", "en");

        // Assert
        result.Should().Be("Open", "English translation for 'Open' status");
    }

    [Fact]
    public void GetEnumTranslation_WithNonExistentEnum_ReturnsFallback()
    {
        // Arrange
        var service = new LocalizationService(_mockLogger.Object, _memoryCache, _mockEnvironment.Object);

        // Act
        var result = service.GetEnumTranslation("NonExistentEnum", "Value", "pl");

        // Assert
        result.Should().Be("Value", "should return enum value as fallback");
    }

    [Fact]
    public void GetEnumTranslation_WithNonExistentValue_ReturnsFallback()
    {
        // Arrange
        var service = new LocalizationService(_mockLogger.Object, _memoryCache, _mockEnvironment.Object);

        // Act
        var result = service.GetEnumTranslation("TicketStatus", "NonExistentValue", "pl");

        // Assert
        result.Should().Be("NonExistentValue", "should return enum value as fallback");
    }

    [Fact]
    public void GetEnumTranslation_CalledMultipleTimes_UsesCachedData()
    {
        // Arrange
        var service = new LocalizationService(_mockLogger.Object, _memoryCache, _mockEnvironment.Object);

        // Act - First call loads from file
        var result1 = service.GetEnumTranslation("TicketStatus", "New", "pl");

        // Delete translation file to test caching
        var filePath = Path.Combine(_tempDir, "Localization", "translations.pl.json");
        File.Delete(filePath);

        // Second call should still work (from cache)
        var result2 = service.GetEnumTranslation("TicketStatus", "New", "pl");

        // Assert
        result1.Should().Be("Nowe");
        result2.Should().Be("Nowe", "should return cached value even after file deletion");
    }

    #endregion

    #region GetBilingualEnum Tests

    [Fact]
    public void GetBilingualEnum_WithValidEnum_ReturnsBilingualFormat()
    {
        // Arrange
        var service = new LocalizationService(_mockLogger.Object, _memoryCache, _mockEnvironment.Object);

        // Act
        var result = service.GetBilingualEnum("TicketStatus", "New");

        // Assert
        result.Should().Be("Nowe / New", "should return bilingual format 'Polish / English'");
    }

    [Fact]
    public void GetBilingualEnum_WithDifferentEnumType_ReturnsCorrectTranslation()
    {
        // Arrange
        var service = new LocalizationService(_mockLogger.Object, _memoryCache, _mockEnvironment.Object);

        // Act
        var result = service.GetBilingualEnum("TicketPriority", "High");

        // Assert
        result.Should().Be("Wysoki / High", "should return bilingual priority translation");
    }

    [Fact]
    public void GetBilingualEnum_WithNonExistentEnum_ReturnsFallbackFormat()
    {
        // Arrange
        var service = new LocalizationService(_mockLogger.Object, _memoryCache, _mockEnvironment.Object);

        // Act
        var result = service.GetBilingualEnum("NonExistent", "Value");

        // Assert
        result.Should().Be("Value / Value", "should return fallback format when enum not found");
    }

    #endregion

    #region GetEmailLabel Tests

    [Fact]
    public void GetEmailLabel_WithValidLabel_ReturnsTranslation()
    {
        // Arrange
        var service = new LocalizationService(_mockLogger.Object, _memoryCache, _mockEnvironment.Object);

        // Act
        var result = service.GetEmailLabel("Hello", "pl");

        // Assert
        result.Should().Be("Witaj", "Polish translation for 'Hello' should be 'Witaj'");
    }

    [Fact]
    public void GetEmailLabel_WithEnglishLocale_ReturnsEnglishTranslation()
    {
        // Arrange
        var service = new LocalizationService(_mockLogger.Object, _memoryCache, _mockEnvironment.Object);

        // Act
        var result = service.GetEmailLabel("TicketId", "en");

        // Assert
        result.Should().Be("Ticket ID", "English translation for 'TicketId' label");
    }

    [Fact]
    public void GetEmailLabel_WithNonExistentLabel_ReturnsFallback()
    {
        // Arrange
        var service = new LocalizationService(_mockLogger.Object, _memoryCache, _mockEnvironment.Object);

        // Act
        var result = service.GetEmailLabel("NonExistentLabel", "pl");

        // Assert
        result.Should().Be("NonExistentLabel", "should return label key as fallback");
    }

    #endregion

    #region GetAllTranslations Tests

    [Fact]
    public void GetAllTranslations_WithValidLocale_ReturnsAllTranslations()
    {
        // Arrange
        var service = new LocalizationService(_mockLogger.Object, _memoryCache, _mockEnvironment.Object);

        // Act
        var result = service.GetAllTranslations("pl");

        // Assert
        result.Should().NotBeEmpty("should return translations");
        result.Should().ContainKey("TicketStatus", "should include TicketStatus translations");
        result.Should().ContainKey("TicketPriority", "should include TicketPriority translations");
        result["TicketStatus"].Should().ContainKey("New", "should include 'New' status translation");
        result["TicketStatus"]["New"].Should().Be("Nowe", "should have correct Polish translation");
    }

    [Fact]
    public void GetAllTranslations_WithEnglishLocale_ReturnsEnglishTranslations()
    {
        // Arrange
        var service = new LocalizationService(_mockLogger.Object, _memoryCache, _mockEnvironment.Object);

        // Act
        var result = service.GetAllTranslations("en");

        // Assert
        result.Should().NotBeEmpty();
        result["TicketStatus"]["New"].Should().Be("New", "should have English translation");
    }

    [Fact]
    public void GetAllTranslations_WithInvalidLocale_ReturnsFallback()
    {
        // Arrange
        var service = new LocalizationService(_mockLogger.Object, _memoryCache, _mockEnvironment.Object);

        // Act
        var result = service.GetAllTranslations("fr");

        // Assert - Service uses graceful fallback instead of throwing
        result.Should().NotBeNull("should return empty dictionary as fallback when locale file doesn't exist");
    }

    [Fact]
    public void GetAllTranslations_CalledMultipleTimes_UsesCachedData()
    {
        // Arrange
        var service = new LocalizationService(_mockLogger.Object, _memoryCache, _mockEnvironment.Object);

        // Act - First call loads from file
        var result1 = service.GetAllTranslations("pl");

        // Delete translation file to test caching
        var filePath = Path.Combine(_tempDir, "Localization", "translations.pl.json");
        File.Delete(filePath);

        // Second call should still work (from cache)
        var result2 = service.GetAllTranslations("pl");

        // Assert
        result1.Should().BeEquivalentTo(result2, "both calls should return same cached data");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void LocalizationService_WithMissingTranslationFiles_ReturnsFallback()
    {
        // Arrange - Delete all translation files
        Directory.Delete(Path.Combine(_tempDir, "Localization"), true);

        var service = new LocalizationService(_mockLogger.Object, _memoryCache, _mockEnvironment.Object);

        // Act
        var result = service.GetEnumTranslation("TicketStatus", "New", "pl");

        // Assert - Service uses graceful fallback instead of throwing
        result.Should().Be("New", "should return enum value as fallback when translation files are missing");
    }

    [Fact]
    public void LocalizationService_WithInvalidJson_ReturnsFallback()
    {
        // Arrange - Create invalid JSON file
        var localizationDir = Path.Combine(_tempDir, "Localization");
        var invalidFile = Path.Combine(localizationDir, "translations.invalid.json");
        File.WriteAllText(invalidFile, "{ invalid json }");

        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(x => x.ContentRootPath).Returns(_tempDir);

        var service = new LocalizationService(_mockLogger.Object, _memoryCache, mockEnv.Object);

        // Act
        var result = service.GetAllTranslations("invalid");

        // Assert - Service uses graceful fallback (returns empty dict) instead of throwing
        result.Should().NotBeNull("should return empty dictionary as fallback when JSON is malformed");
    }

    #endregion

    #region Integration Test

    [Fact]
    public void LocalizationService_WithRealFiles_TranslatesAllEnumTypes()
    {
        // Arrange
        var service = new LocalizationService(_mockLogger.Object, _memoryCache, _mockEnvironment.Object);

        // Act & Assert - Test all enum types
        service.GetBilingualEnum("TicketStatus", "New").Should().Be("Nowe / New");
        service.GetBilingualEnum("TicketStatus", "Open").Should().Be("Otwarte / Open");
        service.GetBilingualEnum("TicketStatus", "Resolved").Should().Be("Rozwiązane / Resolved");

        service.GetBilingualEnum("TicketPriority", "Low").Should().Be("Niski / Low");
        service.GetBilingualEnum("TicketPriority", "Medium").Should().Be("Średni / Medium");
        service.GetBilingualEnum("TicketPriority", "High").Should().Be("Wysoki / High");

        service.GetBilingualEnum("TicketCategory", "IT").Should().Be("IT / IT");
        service.GetBilingualEnum("TicketCategory", "Logistics").Should().Be("Logistyka / Logistics");
        service.GetBilingualEnum("TicketCategory", "Administrative").Should().Be("Administracyjne / Administrative");
    }

    #endregion
}
