namespace Stoat.Core.Services;

public interface INavigationService
{
    void NavigateTo<TViewModel>() where TViewModel : class;
    void GoBack();
    bool CanGoBack { get; }
}
