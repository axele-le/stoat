using Stoat.Services.Models;

namespace Stoat.Services.Interfaces;

public interface ILocalizationService
{
    string this[string key] { get; }
    string Format(string key, params object[] args);
    string CurrentLanguage { get; }
    IReadOnlyList<LanguageInfo> AvailableLanguages { get; }
    void SetLanguage(string languageCode);
    event EventHandler? LanguageChanged;
    void Initialize(Dictionary<string, Dictionary<string, string>> translations);
}
