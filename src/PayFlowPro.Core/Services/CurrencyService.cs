using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PayFlowPro.Core.Interfaces;
using PayFlowPro.Data.Context;
using PayFlowPro.Models.Entities;

namespace PayFlowPro.Core.Services;

public class CurrencyService : ICurrencyService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<CurrencyService> _logger;

    // Default currency settings
    private const string DEFAULT_CURRENCY_CODE = "GBP";
    private const string DEFAULT_CULTURE_NAME = "en-GB";
    private const string DEFAULT_CURRENCY_NAME = "British Pound Sterling";

    public CurrencyService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<CurrencyService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<string> GetCurrencyCodeAsync()
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var setting = await context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == "Currency.Code" && s.Category == "Localization");

            return setting?.Value ?? DEFAULT_CURRENCY_CODE;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve currency code from settings. Using default: {DefaultCode}", DEFAULT_CURRENCY_CODE);
            return DEFAULT_CURRENCY_CODE;
        }
    }

    public async Task<string> GetCurrencySymbolAsync()
    {
        try
        {
            var cultureInfo = await GetCurrencyFormatAsync();
            return cultureInfo.NumberFormat.CurrencySymbol;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve currency symbol. Using default.");
            return "£"; // Default to GBP symbol
        }
    }

    public async Task<CultureInfo> GetCurrencyFormatAsync()
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var setting = await context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == "Currency.Culture" && s.Category == "Localization");

            var cultureName = setting?.Value ?? DEFAULT_CULTURE_NAME;
            return new CultureInfo(cultureName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve currency culture from settings. Using default: {DefaultCulture}", DEFAULT_CULTURE_NAME);
            return new CultureInfo(DEFAULT_CULTURE_NAME);
        }
    }

    public async Task<string> FormatCurrencyAsync(decimal amount)
    {
        try
        {
            var cultureInfo = await GetCurrencyFormatAsync();
            return amount.ToString("C", cultureInfo);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to format currency. Using default format.");
            return amount.ToString("C", new CultureInfo(DEFAULT_CULTURE_NAME));
        }
    }

    public async Task<string> FormatCurrencyWithCodeAsync(decimal amount)
    {
        try
        {
            var formattedAmount = await FormatCurrencyAsync(amount);
            var currencyCode = await GetCurrencyCodeAsync();
            return $"{formattedAmount} ({currencyCode})";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to format currency with code.");
            return amount.ToString("C");
        }
    }

    public async Task<string> GetCurrencyNameAsync()
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var setting = await context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == "Currency.Name" && s.Category == "Localization");

            return setting?.Value ?? DEFAULT_CURRENCY_NAME;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve currency name from settings. Using default: {DefaultName}", DEFAULT_CURRENCY_NAME);
            return DEFAULT_CURRENCY_NAME;
        }
    }

    public async Task SetCurrencyAsync(string currencyCode, string cultureName)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();

            // Validate the culture
            var culture = new CultureInfo(cultureName);
            var currencyName = GetCurrencyDisplayName(currencyCode);

            // Update or create currency code setting
            var codeSetting = await context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == "Currency.Code" && s.Category == "Localization");

            if (codeSetting == null)
            {
                codeSetting = new SystemSetting
                {
                    Key = "Currency.Code",
                    Category = "Localization",
                    Description = "ISO 4217 currency code (e.g., GBP, USD, EUR)",
                    DataType = "String"
                };
                context.SystemSettings.Add(codeSetting);
            }

            codeSetting.Value = currencyCode.ToUpperInvariant();
            codeSetting.UpdatedAt = DateTime.UtcNow;

            // Update or create culture setting
            var cultureSetting = await context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == "Currency.Culture" && s.Category == "Localization");

            if (cultureSetting == null)
            {
                cultureSetting = new SystemSetting
                {
                    Key = "Currency.Culture",
                    Category = "Localization",
                    Description = "Culture name for currency formatting (e.g., en-GB, en-US)",
                    DataType = "String"
                };
                context.SystemSettings.Add(cultureSetting);
            }

            cultureSetting.Value = cultureName;
            cultureSetting.UpdatedAt = DateTime.UtcNow;

            // Update or create currency name setting
            var nameSetting = await context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == "Currency.Name" && s.Category == "Localization");

            if (nameSetting == null)
            {
                nameSetting = new SystemSetting
                {
                    Key = "Currency.Name",
                    Category = "Localization",
                    Description = "Full currency name for display",
                    DataType = "String"
                };
                context.SystemSettings.Add(nameSetting);
            }

            nameSetting.Value = currencyName;
            nameSetting.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            _logger.LogInformation("Currency settings updated: {CurrencyCode} ({CultureName})", currencyCode, cultureName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set currency settings for {CurrencyCode} ({CultureName})", currencyCode, cultureName);
            throw;
        }
    }

    private static string GetCurrencyDisplayName(string currencyCode)
    {
        return currencyCode.ToUpperInvariant() switch
        {
            "GBP" => "British Pound Sterling",
            "USD" => "US Dollar",
            "EUR" => "Euro",
            "INR" => "Indian Rupee",
            "CAD" => "Canadian Dollar",
            "AUD" => "Australian Dollar",
            "JPY" => "Japanese Yen",
            "CHF" => "Swiss Franc",
            "CNY" => "Chinese Yuan",
            "SGD" => "Singapore Dollar",
            _ => $"{currencyCode} Currency"
        };
    }
}