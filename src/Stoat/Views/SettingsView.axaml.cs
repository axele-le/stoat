using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Stoat.Core.ViewModels;
using Stoat.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Stoat.Views;

public partial class SettingsView : UserControl
{
    private SettingsViewModel? _viewModel;
    private IClipboardService? _clipboardService;
    private ILocalizationService? _localizationService;

    public SettingsView()
    {
        InitializeComponent();

        _viewModel = App.Services?.GetRequiredService<SettingsViewModel>();
        _clipboardService = App.Services?.GetService<IClipboardService>();
        _localizationService = App.Services?.GetService<ILocalizationService>();

        DataContext = _viewModel;

        if (_viewModel != null)
        {
            _viewModel.SaveExportFileRequested += OnSaveExportFileRequested;
            _viewModel.OpenImportFileRequested += OnOpenImportFileRequested;
            _viewModel.AutoLockSettingsChanged += OnAutoLockSettingsChanged;
            _viewModel.CopyRecoveryKeyRequested += OnCopyRecoveryKeyRequested;
            _viewModel.ClipboardSettingsChanged += OnClipboardSettingsChanged;
            _viewModel.ThemeChanged += OnThemeChanged;
        }

        _viewModel?.OnNavigatedTo();
    }

    private void TabProfili_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.ActiveTabIndex = 0;
        }
    }

    private void TabClient_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.ActiveTabIndex = 1;
        }
    }

    private void TabExportImport_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.ActiveTabIndex = 2;
        }
    }

    private void SubTabExport_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.IsExportSubTabActive = true;
        }
    }

    private void SubTabImport_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.IsExportSubTabActive = false;
        }
    }

    private void ConflictRename_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel != null) _viewModel.ConflictResolutionIndex = 0;
    }

    private void ConflictReplace_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel != null) _viewModel.ConflictResolutionIndex = 1;
    }

    private void ConflictSkip_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel != null) _viewModel.ConflictResolutionIndex = 2;
    }

    private async void OnCopyRecoveryKeyRequested(object? sender, EventArgs e)
    {
        if (_viewModel == null || string.IsNullOrEmpty(_viewModel.RecoveryKeyDisplay)) return;

        if (_clipboardService != null)
        {
            await _clipboardService.CopyAsync(_viewModel.RecoveryKeyDisplay);
        }
    }

    private void OnClipboardSettingsChanged(object? sender, ClipboardSettingsEventArgs e)
    {
        _clipboardService?.UpdateSettings(e.Enabled, e.TimeoutSeconds);
    }

    private static void OnThemeChanged(object? sender, int themeIndex)
    {
        if (Avalonia.Application.Current is App app)
        {
            app.ApplyTheme(themeIndex);
        }
    }

    private async void OnSaveExportFileRequested(object? sender, string json)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = _localizationService?.Format("Settings.Export.Title") ?? string.Empty,
                SuggestedFileName = $"Stoat_Export_{DateTime.Now:yyyyMMdd_HHmm}",
                DefaultExtension = "stx",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Stoat Export") { Patterns = new[] { "*.stx" } },
                    new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } }
                }
            });

            if (file != null)
            {
                await using var stream = await file.OpenWriteAsync();
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(json);

                _viewModel?.SetExportFileSaveResult(true,
                    _localizationService?.Format("Settings.Export.Success.FileName", file.Name) ?? string.Empty);
            }
        }
        catch (Exception ex)
        {
            _viewModel?.SetExportFileSaveResult(false,
                _localizationService?.Format("Common.Error.SaveFailed", ex.Message) ?? string.Empty);
        }
    }

    private void OnAutoLockSettingsChanged(object? sender, Core.ViewModels.AutoLockSettingsEventArgs e)
    {
        var mainWindow = TopLevel.GetTopLevel(this) as MainWindow;
        mainWindow?.OnAutoLockSettingsChanged(sender, e);
    }

    private async void OnOpenImportFileRequested(object? sender, EventArgs e)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = _localizationService?.Format("Settings.Import.Title") ?? string.Empty,
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType(_localizationService?.Format("Settings.Import.FilePicker.ClientExport") ?? string.Empty) { Patterns = new[] { "*.stx" } },
                    new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } },
                    new FilePickerFileType(_localizationService?.Format("Settings.Import.FilePicker.AllFiles") ?? string.Empty) { Patterns = new[] { "*" } }
                }
            });

            var file = files?.FirstOrDefault();
            if (file != null)
            {
                await using var stream = await file.OpenReadAsync();
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                _viewModel?.SetImportPackageData(json, file.Name);
            }
        }
        catch (Exception ex)
        {
            _viewModel?.SetStatusMessage(false, _localizationService?.Format("Common.Error.OpenFile", ex.Message) ?? string.Empty);
        }
    }
}
