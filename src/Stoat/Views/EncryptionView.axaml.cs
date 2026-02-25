using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Stoat.Core.ViewModels;
using Stoat.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Stoat.Views;

public partial class EncryptionView : UserControl
{
    private EncryptionViewModel? _viewModel;
    private IClipboardService? _clipboardService;
    private ILocalizationService? _localizationService;

    public EncryptionView()
    {
        InitializeComponent();

        _viewModel = App.Services?.GetRequiredService<EncryptionViewModel>();
        _clipboardService = App.Services?.GetService<IClipboardService>();
        _localizationService = App.Services?.GetService<ILocalizationService>();
        
        DataContext = _viewModel;

        if (_viewModel != null)
        {
            _viewModel.ClipboardCopyRequested += OnClipboardCopyRequested;
            _viewModel.SelectFileRequested += OnSelectFileRequested;
        }

        // Setup drag & drop
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _viewModel?.OnNavigatedTo();
    }

    private async void OnClipboardCopyRequested(string text)
    {
        if (_clipboardService != null)
        {
            await _clipboardService.CopyAsync(text);
        }
    }

    private async void OnSelectFileRequested()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var title = _viewModel?.IsEncryptMode == true
            ? _localizationService?.Format("Encryption.Encrypt.SelectFile")
            : _localizationService?.Format("Encryption.Decrypt.SelectFile");

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        if (files.Count > 0)
        {
            _viewModel?.SetSelectedFile(files[0].Path.LocalPath);
        }
    }

#pragma warning disable CS0618 // DragEventArgs.Data is obsolete
    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Files)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var files = e.Data.GetFiles()?.ToList();
        if (files == null || files.Count == 0) return;

        var filePath = files[0].Path.LocalPath;
        _viewModel?.SetSelectedFile(filePath);

        // Switch to file mode if not already
        if (_viewModel != null && !_viewModel.IsFileMode)
        {
            _viewModel.SetFileModeCommand.Execute(null);
        }
    }
#pragma warning restore CS0618
}
