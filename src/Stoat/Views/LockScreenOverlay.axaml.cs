using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Stoat.Core.ViewModels;
using Stoat.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Stoat.Views;

public partial class LockScreenOverlay : UserControl
{
    private LockScreenViewModel? _viewModel;
    private IClipboardService? _clipboardService;

    public LockScreenOverlay()
    {
        InitializeComponent();

        _viewModel = App.Services?.GetRequiredService<LockScreenViewModel>();
        _clipboardService = App.Services?.GetService<IClipboardService>();
        DataContext = _viewModel;

        if (_viewModel != null)
        {
            _viewModel.CopyRecoveryKeyRequested += OnCopyRecoveryKeyRequested;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (IsVisible)
        {
            FocusPasswordBox();
        }

        this.GetObservable(IsVisibleProperty).Subscribe(visible =>
        {
            if (visible)
            {
                FocusPasswordBox();
            }
        });
    }

    private void FocusPasswordBox()
    {
        var passwordBox = this.FindControl<TextBox>("LockPasswordBox");
        passwordBox?.Focus();
    }

    private void OnPasswordKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _viewModel?.UnlockCommand.CanExecute(null) == true)
        {
            _viewModel.UnlockCommand.Execute(null);
            e.Handled = true;
        }
    }

    private async void OnCopyRecoveryKeyRequested(object? sender, EventArgs e)
    {
        if (_viewModel == null || string.IsNullOrEmpty(_viewModel.RecoveryKeyDisplay)) return;

        if (_clipboardService != null)
        {
            await _clipboardService.CopyAsync(_viewModel.RecoveryKeyDisplay);
        }
    }
}
