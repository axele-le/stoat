using System.ComponentModel;
using Stoat.Services.Interfaces;

namespace Stoat.Localization;

public class LocalizationManager : INotifyPropertyChanged
{
    public static LocalizationManager Instance { get; } = new();

    private ILocalizationService _service = null!;

    /// <summary>
    /// Fired when the language changes. Used by TranslationValue wrappers
    /// to push PropertyChanged("Value") to their bindings.
    /// </summary>
    internal event Action? LanguageUpdated;

    public string this[string key] => _service[key];

    public void Initialize(ILocalizationService service)
    {
        _service = service;
        _service.LanguageChanged += (_, _) =>
        {
            LanguageUpdated?.Invoke();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        };

        LanguageUpdated?.Invoke();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// Lightweight wrapper around a single translation key.
/// Exposes a bindable Value property that updates on language changes.
/// Used by TranslateExtension to avoid indexer binding paths which
/// Avalonia's reflection binding engine doesn't re-evaluate on PropertyChanged.
/// </summary>
internal sealed class TranslationValue : INotifyPropertyChanged
{
    private readonly string _key;

    public TranslationValue(string key)
    {
        _key = key;
        LocalizationManager.Instance.LanguageUpdated += OnLanguageUpdated;
    }

    public string Value => LocalizationManager.Instance[_key];

    private void OnLanguageUpdated()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
