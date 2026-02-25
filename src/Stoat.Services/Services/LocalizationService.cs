using Stoat.Services.Interfaces;
using Stoat.Services.Models;

namespace Stoat.Services.Services;

/// <summary>
/// Service responsible for managing localization and translations in the application.
/// </summary>
public class LocalizationService : ILocalizationService
{
    private Dictionary<string, Dictionary<string, string>> _translations = new();
    private string _currentLang = "it";
    private const string FallbackLang = "it";

    public static readonly IReadOnlyList<LanguageInfo> Languages = new List<LanguageInfo>
    {
        new() { Code = "it", NativeName = "Italiano" },
        new() { Code = "en", NativeName = "English" },
        new() { Code = "de", NativeName = "Deutsch" },
        new() { Code = "fr", NativeName = "Français" },
        new() { Code = "es", NativeName = "Español" },
        new() { Code = "pl", NativeName = "Polski" }
    };

    public string this[string key]
    {
        get
        {
            if (_translations.TryGetValue(_currentLang, out var currentDict) &&
                currentDict.TryGetValue(key, out var value))
                return value;

            if (_currentLang != FallbackLang &&
                _translations.TryGetValue(FallbackLang, out var fallbackDict) &&
                fallbackDict.TryGetValue(key, out var fallbackValue))
                return fallbackValue;

            return key;
        }
    }

    public string Format(string key, params object[] args)
    {
        var template = this[key];
        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    public string CurrentLanguage => _currentLang;

    public IReadOnlyList<LanguageInfo> AvailableLanguages => Languages;

    public void SetLanguage(string languageCode)
    {
        if (_currentLang == languageCode) return;
        _currentLang = languageCode;
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? LanguageChanged;

    public void Initialize(Dictionary<string, Dictionary<string, string>> translations)
    {
        _translations = translations;
    }
}
