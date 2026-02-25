using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Stoat.Core.ViewModels;
using Stoat.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Stoat.Views;

public partial class PemFormView : UserControl
{
    private readonly ILocalizationService? _localizationService;
    
    public PemFormView()
    {
        InitializeComponent();

        _localizationService = App.Services?.GetService<ILocalizationService>();
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        SetupDragDrop();
        WireImportFileRequested();
    }

    private void SetupDragDrop()
    {
        var dropZone = this.FindControl<Border>("DropZone");
        if (dropZone != null)
        {
            dropZone.AddHandler(DragDrop.DropEvent, OnDrop);
            dropZone.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        }
    }

    private void WireImportFileRequested()
    {
        if (DataContext is PemFormViewModel vm)
        {
            vm.ImportFileRequested += OnImportFileRequested;
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        e.Handled = true;

        var files = e.DataTransfer.TryGetFiles();
        if (files == null) return;

        foreach (var file in files)
        {
            if (file is IStorageFile storageFile)
            {
                try
                {
                    var fileName = storageFile.Name;
                    await using var stream = await storageFile.OpenReadAsync();
                    using var reader = new StreamReader(stream);
                    var content = await reader.ReadToEndAsync();

                    if (DataContext is PemFormViewModel vm)
                    {
                        await vm.HandleImportedFileAsync(fileName, content);
                    }
                }
                catch (Exception ex)
                {
                    if (DataContext is PemFormViewModel vm)
                    {
                        vm.StatusMessage = _localizationService?.Format("Pem.Import.Error.Failed", ex.Message);
                        vm.IsError = true;
                    }
                }
            }
        }
    }

    private async void OnImportFileRequested()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = _localizationService?.Format("PemForm.ImportSection.Title"),
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("PEM Files") { Patterns = new[] { "*.pem" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
            }
        });

        if (files.Count > 0)
        {
            var file = files[0];
            try
            {
                var fileName = file.Name;
                await using var stream = await file.OpenReadAsync();
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();

                if (DataContext is PemFormViewModel vm)
                {
                    await vm.HandleImportedFileAsync(fileName, content);
                }
            }
            catch (Exception ex)
            {
                if (DataContext is PemFormViewModel vm)
                {
                    vm.StatusMessage = _localizationService?.Format("Pem.Import.Error.Failed", ex.Message);
                    vm.IsError = true;
                }
            }
        }
    }
}
