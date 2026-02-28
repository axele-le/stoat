using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Stoat.Services.Interfaces;
using Stoat.Services.Models;

namespace Stoat.Core.ViewModels;

/// <summary>
/// ViewModel for the API Key creation form.
/// </summary>
public partial class ApiKeyFormViewModel : ObservableObject
{
    private readonly IApiKeyStorageService _apiKeyStorageService;
    private readonly ILocalizationService _localizationService;
    private readonly IToastService _toastService;

    /// <summary>
    /// Name for the new API key to generate.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateKeyCommand))]
    private string _newKeyName = string.Empty;

    /// <summary>
    /// Selected type for the new key.
    /// </summary>
    [ObservableProperty]
    private ApiKeyType _selectedType = ApiKeyType.Production;

    /// <summary>
    /// Key length for generation.
    /// </summary>
    [ObservableProperty]
    private int _keyLength = 32;

    /// <summary>
    /// Display string for key length.
    /// </summary>
    [ObservableProperty]
    private string _keyLengthDisplay = "32";

    /// <summary>
    /// Selected complexity for generation.
    /// </summary>
    [ObservableProperty]
    private ApiKeyComplexity _selectedComplexity = ApiKeyComplexity.AlphanumericSymbols;

    /// <summary>
    /// Optional prefix to prepend to the generated key.
    /// </summary>
    [ObservableProperty]
    private string _keyPrefix = string.Empty;

    /// <summary>
    /// Whether key generation is in progress.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateKeyCommand))]
    private bool _isGenerating;

    /// <summary>
    /// Available key types for generation.
    /// </summary>
    public ApiKeyType[] AvailableTypes { get; } = [ApiKeyType.Development, ApiKeyType.Test, ApiKeyType.Production];

    /// <summary>
    /// Available complexity levels.
    /// </summary>
    public ApiKeyComplexity[] AvailableComplexities { get; } =
    [
        ApiKeyComplexity.Numeric,
        ApiKeyComplexity.AlphanumericLower,
        ApiKeyComplexity.Alphanumeric,
        ApiKeyComplexity.AlphanumericSymbols,
        ApiKeyComplexity.Base64UrlSafe
    ];

    /// <summary>
    /// Minimum key length.
    /// </summary>
    public int MinLength { get; } = 8;

    /// <summary>
    /// Maximum key length.
    /// </summary>
    public int MaxLength { get; } = 256;

    /// <summary>
    /// Event raised when the form should be closed.
    /// </summary>
    public event Action? CloseRequested;

    /// <summary>
    /// Event raised when key generation is successful.
    /// </summary>
    public event Action? GenerationSucceeded;

    public ApiKeyFormViewModel(IApiKeyStorageService apiKeyStorageService, ILocalizationService localizationService, IToastService toastService)
    {
        _apiKeyStorageService = apiKeyStorageService;
        _localizationService = localizationService;
        _toastService = toastService;
    }

    partial void OnKeyLengthChanged(int value)
    {
        KeyLengthDisplay = value.ToString();
    }

    [RelayCommand(CanExecute = nameof(CanGenerateKey))]
    private async Task GenerateKeyAsync()
    {
        if (string.IsNullOrWhiteSpace(NewKeyName))
        {
            _toastService.Warning(_localizationService.Format("ApiKey.Form.Error.NoName"));
            return;
        }

        IsGenerating = true;

        try
        {
            var prefix = string.IsNullOrWhiteSpace(KeyPrefix) ? null : KeyPrefix;
            await _apiKeyStorageService.CreateKeyAsync(NewKeyName.Trim(), SelectedType, KeyLength, SelectedComplexity, prefix);
            var typeDisplay = SelectedType == ApiKeyType.Production ? "Production" : SelectedType == ApiKeyType.Test ? "Test" : "Development";
            var complexityName = GetComplexityDisplayName(SelectedComplexity);
            _toastService.Success(_localizationService.Format("ApiKey.Form.Success.Generated", typeDisplay, KeyLength, complexityName));

            GenerationSucceeded?.Invoke();
            ClearForm();
        }
        catch (Exception ex)
        {
            _toastService.Error(_localizationService.Format("Common.Error.WithMessage", ex.Message));
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private bool CanGenerateKey() => !IsGenerating && !string.IsNullOrWhiteSpace(NewKeyName);

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke();
    }

    private void ClearForm()
    {
        NewKeyName = string.Empty;
        KeyLength = 32;
        SelectedType = ApiKeyType.Production;
        SelectedComplexity = ApiKeyComplexity.AlphanumericSymbols;
        KeyPrefix = string.Empty;
    }

    private string GetComplexityDisplayName(ApiKeyComplexity complexity)
    {
        return complexity switch
        {
            ApiKeyComplexity.Numeric => _localizationService.Format("ApiKey.Complexity.Numeric"),
            ApiKeyComplexity.AlphanumericLower => _localizationService.Format("ApiKey.Complexity.AlphanumericLower"),
            ApiKeyComplexity.Alphanumeric => _localizationService.Format("ApiKey.Complexity.Alphanumeric"),
            ApiKeyComplexity.AlphanumericSymbols => _localizationService.Format("ApiKey.Complexity.AlphanumericSymbols"),
            ApiKeyComplexity.Base64UrlSafe => _localizationService.Format("ApiKey.Complexity.Base64UrlSafe"),
            _ => _localizationService.Format("ApiKey.Complexity.Standard")
        };
    }
}
