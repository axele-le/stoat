using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Stoat.Core.ViewModels;
using Stoat.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Stoat.Views;

public partial class HashView : UserControl
{
    private HashViewModel? _viewModel;
    private IClipboardService? _clipboardService;
    private ILocalizationService? _localizationService;

    public HashView()
    {
        InitializeComponent();

        _viewModel = App.Services?.GetRequiredService<HashViewModel>();
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

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = _localizationService?.Format("Hash.SelectFile") ?? string.Empty,
            AllowMultiple = false
        });

        if (files.Count > 0)
        {
            _viewModel?.SetSelectedFile(files[0].Path.LocalPath);
        }
    }

#pragma warning disable CS0618
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

        if (_viewModel != null && !_viewModel.IsFileMode)
        {
            _viewModel.SetFileModeCommand.Execute(null);
        }
    }
#pragma warning restore CS0618
}
