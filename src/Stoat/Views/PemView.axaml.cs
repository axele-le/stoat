using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Stoat.Core.ViewModels;
using Stoat.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace Stoat.Views;

public partial class PemView : UserControl
{
    private PemViewModel? _viewModel;
    private IClipboardService? _clipboardService;
    private ILocalizationService? _localizationService;

    public PemView()
    {
        InitializeComponent();

        _viewModel = App.Services?.GetRequiredService<PemViewModel>();
        _clipboardService = App.Services?.GetService<IClipboardService>();
        _localizationService = App.Services?.GetService<ILocalizationService>();

        DataContext = _viewModel;

        if (_viewModel != null)
        {
            _viewModel.SaveFileRequested += OnSaveFileRequested;
            _viewModel.CopyToClipboardRequested += OnCopyToClipboardRequested;
        }
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _viewModel?.OnNavigatedTo();
    }

    private async void OnSaveFileRequested(string content, string defaultFileName, string title)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = defaultFileName,
            FileTypeChoices = new[]
            {
                new FilePickerFileType(_localizationService?.Format("Pem.SaveFile.Type.Pem")) { Patterns = new[] { "*.pem" } },
                new FilePickerFileType(_localizationService?.Format("Pem.SaveFile.Type.All")) { Patterns = new[] { "*.*" } }
            }
        });

        if (file != null)
        {
            try
            {
                await using var stream = await file.OpenWriteAsync();
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(content);
                _viewModel?.SetFileSaveResult(true, _localizationService?.Format("Common.Success.FileSaveName", file.Name) ?? string.Empty);
            }
            catch (Exception ex)
            {
                _viewModel?.SetFileSaveResult(false, _localizationService?.Format("Common.Error.WithMessage", ex.Message) ?? string.Empty);
            }
        }
        else
        {
            _viewModel?.SetFileSaveResult(false, _localizationService?.Format("Common.Error.SaveCancelled") ?? string.Empty);
        }
    }

    private async void OnCopyToClipboardRequested(string content)
    {
        if (_clipboardService != null)
        {
            await _clipboardService.CopyAsync(content);
        }
    }
}
