using Avalonia.Controls;
using Stoat.Core.ViewModels;
using Stoat.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Stoat.Views;

public partial class ApiKeyView : UserControl
{
    private ApiKeyViewModel? _viewModel;
    private IClipboardService? _clipboardService;

    public ApiKeyView()
    {
        InitializeComponent();

        _viewModel = App.Services?.GetRequiredService<ApiKeyViewModel>();
        _clipboardService = App.Services?.GetService<IClipboardService>();
        DataContext = _viewModel;

        if (_viewModel != null)
        {
            _viewModel.CopyToClipboardRequested += OnCopyToClipboardRequested;
        }
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _viewModel?.OnNavigatedTo();
    }

    private async void OnCopyToClipboardRequested(string text)
    {
        if (_clipboardService != null)
        {
            await _clipboardService.CopyAsync(text);
        }
    }
}
