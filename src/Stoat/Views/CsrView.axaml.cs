using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Stoat.Core.ViewModels;
using Stoat.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Stoat.Views;

public partial class CsrView : UserControl
{
    private CsrViewModel? _viewModel;
    private IClipboardService? _clipboardService;
    private ILocalizationService? _localizationService;

    private static readonly string[] CsrExtensions = { ".csr", ".pem" };
    private static readonly string[] KeyExtensions = { ".key" };

    public CsrView()
    {
        InitializeComponent();

        _viewModel = App.Services?.GetRequiredService<CsrViewModel>();
        _clipboardService = App.Services?.GetService<IClipboardService>();
        _localizationService = App.Services?.GetService<ILocalizationService>();

        DataContext = _viewModel;

        if (_viewModel != null)
        {
            _viewModel.SaveFileRequested += OnSaveFileRequested;
            _viewModel.CopyToClipboardRequested += OnCopyToClipboardRequested;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        // Setup drag & drop for import
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _viewModel?.OnNavigatedTo();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CsrViewModel.ImportFormViewModel))
        {
            WireImportFormEvents();
        }
    }

    private void WireImportFormEvents()
    {
        var importForm = _viewModel?.ImportFormViewModel;
        if (importForm == null) return;

        importForm.SelectCsrFileRequested += OnSelectCsrFileRequested;
        importForm.SelectKeyFileRequested += OnSelectKeyFileRequested;
    }

    private async void OnSelectCsrFileRequested()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = _localizationService?.Format("Csr.Import.SelectFile") ?? string.Empty,
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("CSR Files") { Patterns = new[] { "*.csr", "*.pem" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
            }
        });

        if (files.Count > 0 && _viewModel?.ImportFormViewModel != null)
        {
            await _viewModel.ImportFormViewModel.SetCsrFileAsync(files[0].Path.LocalPath);
        }
    }

    private async void OnSelectKeyFileRequested()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = _localizationService?.Format("Csr.Import.SelectKeyFile") ?? string.Empty,
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Key Files") { Patterns = new[] { "*.key", "*.pem" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
            }
        });

        if (files.Count > 0 && _viewModel?.ImportFormViewModel != null)
        {
            await _viewModel.ImportFormViewModel.SetKeyFileAsync(files[0].Path.LocalPath);
        }
    }

#pragma warning disable CS0618 // DragEventArgs.Data is obsolete
    private void OnDragOver(object? sender, DragEventArgs e)
    {
        // Only accept drops when import form is open
        if (_viewModel?.IsImportFormOpen != true)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        e.DragEffects = e.Data.Contains(DataFormats.Files)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (_viewModel?.IsImportFormOpen != true || _viewModel.ImportFormViewModel == null) return;

        var files = e.Data.GetFiles()?.ToList();
        if (files == null || files.Count == 0) return;

        var importForm = _viewModel.ImportFormViewModel;

        foreach (var file in files)
        {
            var filePath = file.Path.LocalPath;
            var ext = Path.GetExtension(filePath).ToLowerInvariant();

            if (KeyExtensions.Contains(ext))
            {
                // .key files always go to key
                if (importForm.ImportCsrWithKey)
                {
                    await importForm.SetKeyFileAsync(filePath);
                }
            }
            else if (CsrExtensions.Contains(ext))
            {
                // .csr and .pem — if CSR slot is empty, fill it; otherwise try key slot
                if (!importForm.HasCsrFile)
                {
                    await importForm.SetCsrFileAsync(filePath);
                }
                else if (importForm.ImportCsrWithKey && !importForm.HasKeyFile)
                {
                    await importForm.SetKeyFileAsync(filePath);
                }
            }
        }
    }
#pragma warning restore CS0618

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
                new FilePickerFileType("CSR/Key File") { Patterns = new[] { "*.csr", "*.key", "*.pem" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
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
