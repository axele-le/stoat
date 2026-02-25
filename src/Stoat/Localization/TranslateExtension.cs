using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace Stoat.Localization;

public class TranslateExtension : MarkupExtension
{
    public string Key { get; }

    public TranslateExtension(string key) => Key = key;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        // Create a per-key wrapper with a simple "Value" property.
        // Binding to a named property (not an indexer) ensures Avalonia's
        // reflection binding engine correctly re-evaluates on PropertyChanged.
        var source = new TranslationValue(Key);
        return new Binding(nameof(TranslationValue.Value))
        {
            Source = source,
            Mode = BindingMode.OneWay
        };
    }
}
