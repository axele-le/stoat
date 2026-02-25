using Avalonia.Controls;
using Stoat.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Stoat.Views;

public partial class HomeView : UserControl
{
    private HomeViewModel? _viewModel;

    public HomeView()
    {
        InitializeComponent();

        _viewModel = App.Services?.GetRequiredService<HomeViewModel>();
        DataContext = _viewModel;

        if (_viewModel != null)
        {
            _viewModel.NavigationRequested += OnNavigationRequested;
        }
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _viewModel?.OnNavigatedTo();
    }

    private void OnNavigationRequested(string target)
    {
        var mainWindow = TopLevel.GetTopLevel(this) as MainWindow;
        mainWindow?.NavigateToView(target);
    }
}
