using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Stoat.Services.Interfaces;
using Stoat.Services.Models;

namespace Stoat.Core.ViewModels;

public partial class ApiKeyViewModel : ViewModelBase
{
    private readonly IApiKeyStorageService _apiKeyStorageService;
    private readonly IConfirmationService _confirmationService;
    private readonly ILocalizationService _localizationService;
    private readonly IToastService _toastService;

    /// <summary>
    /// Collection of saved API keys.
    /// </summary>
    public ObservableCollection<ApiKeyItemViewModel> Keys { get; } = new();

    /// <summary>
    /// Whether there are any saved API keys.
    /// </summary>
    [ObservableProperty]
    private bool _hasKeys;

    /// <summary>
    /// Whether the form is currently open.
    /// </summary>
    [ObservableProperty]
    private bool _isFormOpen;

    /// <summary>
    /// The form ViewModel for creating API keys.
    /// </summary>
    [ObservableProperty]
    private ApiKeyFormViewModel? _formViewModel;

    public ApiKeyViewModel(IApiKeyStorageService apiKeyStorageService, IConfirmationService confirmationService, ILocalizationService localizationService, IToastService toastService)
    {
        _apiKeyStorageService = apiKeyStorageService;
        _confirmationService = confirmationService;
        _localizationService = localizationService;
        _toastService = toastService;

        // Subscribe to collection events
        _apiKeyStorageService.KeyCreated += OnKeyCreated;
        _apiKeyStorageService.KeyDeleted += OnKeyDeleted;
    }

    public override async void OnNavigatedTo()
    {
        base.OnNavigatedTo();
        await RunWithMinimumLoadingTime(LoadKeysAsync);
    }

    private async Task LoadKeysAsync()
    {
        try
        {
            var keys = await _apiKeyStorageService.GetAllKeysAsync();
            Keys.Clear();

            foreach (var key in keys)
            {
                Keys.Add(await MapToViewModelAsync(key));
            }

            HasKeys = Keys.Count > 0;
        }
        catch (Exception ex)
        {
            _toastService.Error(_localizationService.Format("Common.Error.LoadingFailed", ex.Message));
        }
    }

    private async void OnKeyCreated(object? sender, ApiKey key)
    {
        // Insert at beginning (newest first)
        Keys.Insert(0, await MapToViewModelAsync(key));
        HasKeys = true;
    }

    private void OnKeyDeleted(object? sender, Guid id)
    {
        var item = Keys.FirstOrDefault(k => k.Id == id);
        if (item != null)
        {
            Keys.Remove(item);
        }
        HasKeys = Keys.Count > 0;
    }

    private async Task<ApiKeyItemViewModel> MapToViewModelAsync(ApiKey key)
    {
        // Get decrypted value for masked preview
        var decryptedValue = await _apiKeyStorageService.GetDecryptedValueAsync(key.Id);
        var maskedPreview = key.GetMaskedPreview(decryptedValue);

        return new ApiKeyItemViewModel
        {
            Id = key.Id,
            Name = key.Name,
            Type = key.Type,
            Length = key.Length,
            Complexity = key.Complexity,
            MaskedPreview = maskedPreview,
            CreatedAtUtc = key.CreatedAtUtc
        };
    }

    [RelayCommand]
    private void OpenNewApiKeyForm()
    {
        FormViewModel = new ApiKeyFormViewModel(_apiKeyStorageService, _localizationService, _toastService);
        FormViewModel.CloseRequested += () =>
        {
            IsFormOpen = false;
            FormViewModel = null;
        };
        FormViewModel.GenerationSucceeded += () =>
        {
            IsFormOpen = false;
            FormViewModel = null;
            _toastService.Success(_localizationService.Format("ApiKey.Success.Generated"));
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
    private async Task DeleteKeyAsync(Guid id)
    {
        if (!await _confirmationService.ConfirmAsync(_localizationService.Format("ApiKey.Delete.Dialog.Title"), _localizationService.Format("ApiKey.Delete.Dialog.Confirm")))
            return;

        try
        {
            await _apiKeyStorageService.DeleteKeyAsync(id);
            _toastService.Success(_localizationService.Format("ApiKey.Success.Deleted"));
        }
        catch (Exception ex)
        {
            _toastService.Error(_localizationService.Format("Common.Error.DeletionFailed", ex.Message));
        }
    }

    [RelayCommand]
    private async Task CopyKeyAsync(Guid id)
    {
        try
        {
            var decryptedValue = await _apiKeyStorageService.GetDecryptedValueAsync(id);
            CopyToClipboardRequested?.Invoke(decryptedValue);
            _toastService.Success(_localizationService.Format("ApiKey.Success.Copied"));
        }
        catch (Exception ex)
        {
            _toastService.Error(_localizationService.Format("Common.Error.WithMessage", ex.Message));
        }
    }

    // Events for View to handle platform-specific operations
    public event Action<string>? CopyToClipboardRequested;
}
