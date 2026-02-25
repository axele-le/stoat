using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Stoat.Services.Interfaces;
using Stoat.Services.Models;

namespace Stoat.Core.ViewModels;

public partial class PemViewModel : ViewModelBase
{
    private readonly IPemService _pemService;
    private readonly IPemStorageService _pemStorageService;
    private readonly IConfirmationService _confirmationService;
    private readonly ILocalizationService _localizationService;

    public ObservableCollection<PemKeyGroupViewModel> KeyPairs { get; } = new();

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private bool _hasKeyPairs;

    [ObservableProperty]
    private bool _isFormOpen;

    [ObservableProperty]
    private PemFormViewModel? _formViewModel;

    public PemViewModel(
        IPemService pemService,
        IPemStorageService pemStorageService,
        IConfirmationService confirmationService,
        ILocalizationService localizationService)
    {
        _pemService = pemService;
        _pemStorageService = pemStorageService;
        _confirmationService = confirmationService;
        _localizationService = localizationService;

        _pemStorageService.KeyPairCreated += OnKeyPairCreated;
        _pemStorageService.KeyPairDeleted += OnKeyPairDeleted;
    }

    public override async void OnNavigatedTo()
    {
        base.OnNavigatedTo();
        await RunWithMinimumLoadingTime(LoadKeyPairsAsync);
    }

    private async Task LoadKeyPairsAsync()
    {
        try
        {
            var keyPairs = await _pemStorageService.GetAllKeyPairsAsync();
            KeyPairs.Clear();

            foreach (var kp in keyPairs)
            {
                KeyPairs.Add(MapToGroupViewModel(kp));
            }

            HasKeyPairs = KeyPairs.Count > 0;
        }
        catch (Exception ex)
        {
            SetStatus(_localizationService.Format("Common.Error.LoadingFailed", ex.Message), isError: true);
        }
    }

    private void OnKeyPairCreated(object? sender, PemKeyPair keyPair)
    {
        KeyPairs.Insert(0, MapToGroupViewModel(keyPair));
        HasKeyPairs = true;
    }

    private void OnKeyPairDeleted(object? sender, Guid id)
    {
        var item = KeyPairs.FirstOrDefault(k => k.Id == id);
        if (item != null)
        {
            KeyPairs.Remove(item);
        }
        HasKeyPairs = KeyPairs.Count > 0;
    }

    private PemKeyGroupViewModel MapToGroupViewModel(PemKeyPair kp)
    {
        return new PemKeyGroupViewModel
        {
            ParentViewModel = this,
            Id = kp.Id,
            Name = kp.Name,
            Algorithm = kp.Algorithm,
            PublicKey = kp.PublicKey,
            CreatedAtUtc = kp.CreatedAtUtc,
            HasPrivateKey = kp.HasPrivateKey
        };
    }

    [RelayCommand]
    private void OpenNewPemForm()
    {
        FormViewModel = new PemFormViewModel(_pemStorageService, _pemService, _localizationService);
        FormViewModel.CloseRequested += () =>
        {
            IsFormOpen = false;
            FormViewModel = null;
        };
        FormViewModel.GenerationSucceeded += () =>
        {
            IsFormOpen = false;
            FormViewModel = null;
            SetStatus(_localizationService.Format("Pem.Success.OperationCompleted"));
        };

        IsFormOpen = true;
    }

    [RelayCommand]
    private void CloseForm()
    {
        IsFormOpen = false;
        FormViewModel = null;
    }

    [RelayCommand]
    private async Task DeleteKeyPairAsync(Guid id)
    {
        if (!await _confirmationService.ConfirmAsync(_localizationService.Format("Pem.Delete.Dialog.Title"), _localizationService.Format("Pem.Delete.Dialog.Confirm")))
            return;

        try
        {
            await _pemStorageService.DeleteKeyPairAsync(id);
            SetStatus(_localizationService.Format("Pem.Success.Deleted"));
        }
        catch (Exception ex)
        {
            SetStatus(_localizationService.Format("Common.Error.DeletionFailed", ex.Message), isError: true);
        }
    }

    [RelayCommand]
    private async Task ToggleShowGroupPrivateKeyAsync(PemKeyGroupViewModel group)
    {
        if (!group.HasPrivateKey) return;

        if (group.ShowPrivateKeyPanel)
        {
            group.ShowPrivateKeyPanel = false;
        }
        else
        {
            if (string.IsNullOrEmpty(group.PrivateKey))
            {
                group.IsLoadingPrivateKey = true;
                try
                {
                    group.PrivateKey = await _pemStorageService.GetDecryptedPrivateKeyAsync(group.Id);
                }
                catch (Exception ex)
                {
                    SetStatus(_localizationService.Format("Common.Error.WithMessage", ex.Message), isError: true);
                    return;
                }
                finally
                {
                    group.IsLoadingPrivateKey = false;
                }
            }
            group.ShowPrivateKeyPanel = true;
        }
    }

    [RelayCommand]
    private async Task ToggleShowPrivateKeyAsync(PemKeyGroupViewModel group)
    {
        if (!group.HasPrivateKey) return;

        if (group.ShowPrivateKey)
        {
            group.ShowPrivateKey = false;
        }
        else
        {
            if (string.IsNullOrEmpty(group.PrivateKey))
            {
                group.IsLoadingPrivateKey = true;
                try
                {
                    group.PrivateKey = await _pemStorageService.GetDecryptedPrivateKeyAsync(group.Id);
                }
                catch (Exception ex)
                {
                    SetStatus(_localizationService.Format("Common.Error.WithMessage", ex.Message), isError: true);
                    return;
                }
                finally
                {
                    group.IsLoadingPrivateKey = false;
                }
            }
            group.ShowPrivateKey = true;
        }
    }

    [RelayCommand]
    private void ToggleShowPublicKey(PemKeyGroupViewModel group)
    {
        group.ShowPublicKey = !group.ShowPublicKey;
    }

    [RelayCommand]
    private async Task CopyGroupPrivateKeyAsync(PemKeyGroupViewModel group)
    {
        if (!group.HasPrivateKey) return;

        try
        {
            string privateKey;
            if (!string.IsNullOrEmpty(group.PrivateKey))
            {
                privateKey = group.PrivateKey;
            }
            else
            {
                privateKey = await _pemStorageService.GetDecryptedPrivateKeyAsync(group.Id);
            }

            CopyToClipboardRequested?.Invoke(privateKey);

            group.IsPrivateKeyCopied = true;
            SetStatus(_localizationService.Format("Pem.Success.PrivateKeyCopied"));

            _ = Task.Delay(2000).ContinueWith(_ => group.IsPrivateKeyCopied = false);
        }
        catch (Exception ex)
        {
            SetStatus(_localizationService.Format("Common.Error.WithMessage", ex.Message), isError: true);
        }
    }

    [RelayCommand]
    private async Task CopyPrivateKeyAsync(PemKeyGroupViewModel group)
    {
        await CopyGroupPrivateKeyAsync(group);
    }

    [RelayCommand]
    private void CopyPublicKey(PemKeyGroupViewModel group)
    {
        CopyToClipboardRequested?.Invoke(group.PublicKey);

        group.IsPublicKeyCopied = true;
        SetStatus(_localizationService.Format("Pem.Success.PublicKeyCopied"));

        _ = Task.Delay(2000).ContinueWith(_ => group.IsPublicKeyCopied = false);
    }

    [RelayCommand]
    private async Task DownloadGroupPrivateKeyAsync(PemKeyGroupViewModel group)
    {
        if (!group.HasPrivateKey) return;

        try
        {
            string privateKey;
            if (!string.IsNullOrEmpty(group.PrivateKey))
            {
                privateKey = group.PrivateKey;
            }
            else
            {
                privateKey = await _pemStorageService.GetDecryptedPrivateKeyAsync(group.Id);
            }

            var fileName = $"{SanitizeFileName(group.Name)}_private.pem";
            SaveFileRequested?.Invoke(privateKey, fileName, _localizationService.Format("Pem.File.SavePrivateKeyTitle"));
        }
        catch (Exception ex)
        {
            SetStatus(_localizationService.Format("Common.Error.WithMessage", ex.Message), isError: true);
        }
    }

    [RelayCommand]
    private async Task DownloadPrivateKeyAsync(PemKeyGroupViewModel group)
    {
        await DownloadGroupPrivateKeyAsync(group);
    }

    [RelayCommand]
    private void DownloadPublicKey(PemKeyGroupViewModel group)
    {
        var fileName = $"{SanitizeFileName(group.Name)}_public.pem";
        SaveFileRequested?.Invoke(group.PublicKey, fileName, _localizationService.Format("Pem.File.SavePublicKeyTitle"));
    }

    public void SetFileSaveResult(bool success, string? message = null)
    {
        SetStatus(message ?? (success ? _localizationService.Format("Common.Success.FileSaved") : _localizationService.Format("Common.Error.SaveCancelled")), isError: !success);
    }

    private void SetStatus(string message, bool isError = false)
    {
        StatusMessage = message;
        IsError = isError;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }

    // Events for View to handle platform-specific operations
    public event Action<string, string, string>? SaveFileRequested;
    public event Action<string>? CopyToClipboardRequested;
}
