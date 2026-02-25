using CommunityToolkit.Mvvm.ComponentModel;
using Stoat.Services.Interfaces;

namespace Stoat.Core.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading = true;

    protected ILocalizationService? Loc { get; private set; }

    protected void InitializeLocalization(ILocalizationService loc, Action onLanguageChanged)
    {
        Loc = loc;
        loc.LanguageChanged += (_, _) => onLanguageChanged();
    }

    public virtual void OnNavigatedTo() { }
    public virtual void OnNavigatedFrom() { }

    protected async Task RunWithMinimumLoadingTime(Func<Task> action, int minMs = 500)
    {
        IsLoading = true;
        try
        {
            await Task.WhenAll(action(), Task.Delay(minMs));
        }
        finally
        {
            IsLoading = false;
        }
    }
}
