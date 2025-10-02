using System.Globalization;

namespace PayFlowPro.Core.Interfaces;

public interface ICurrencyService
{
    Task<string> GetCurrencyCodeAsync();
    Task<string> GetCurrencySymbolAsync();
    Task<CultureInfo> GetCurrencyFormatAsync();
    Task<string> FormatCurrencyAsync(decimal amount);
    Task<string> FormatCurrencyWithCodeAsync(decimal amount);
    Task SetCurrencyAsync(string currencyCode, string cultureName);
    Task<string> GetCurrencyNameAsync();
}