using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Stoat.Services.Interfaces;

namespace Stoat.Core.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly IPemStorageService _pemStorageService;
    private readonly IApiKeyStorageService _apiKeyStorageService;
    private readonly ICsrStorageService _csrStorageService;

    [ObservableProperty]
    private int _pemKeyCount;

    [ObservableProperty]
    private int _csrCount;

    [ObservableProperty]
    private int _apiKeyCount;

    [ObservableProperty]
    private int _encryptedFilesCount;

    public event Action<string>? NavigationRequested;

    public HomeViewModel(
        IPemStorageService pemStorageService,
        IApiKeyStorageService apiKeyStorageService,
        ICsrStorageService csrStorageService)
    {
        _pemStorageService = pemStorageService;
        _apiKeyStorageService = apiKeyStorageService;
        _csrStorageService = csrStorageService;
    }

    public override async void OnNavigatedTo()
    {
        base.OnNavigatedTo();
        await RunWithMinimumLoadingTime(LoadCountsAsync);
    }

    private async Task LoadCountsAsync()
    {
        try
        {
            var pemKeys = await _pemStorageService.GetAllKeyPairsAsync();
            PemKeyCount = pemKeys.Count;

            var apiKeys = await _apiKeyStorageService.GetAllKeysAsync();
            ApiKeyCount = apiKeys.Count;

            var csrs = await _csrStorageService.GetAllCsrsAsync();
            CsrCount = csrs.Count;
        }
        catch
        {
            // Counts remain at default 0
        }
    }

    [RelayCommand]
    private void NavigateToPem() => NavigationRequested?.Invoke("PEM");

    [RelayCommand]
    private void NavigateToCsr() => NavigationRequested?.Invoke("CSR");

    [RelayCommand]
    private void NavigateToEncrypt() => NavigationRequested?.Invoke("Encryption");

    [RelayCommand]
    private void NavigateToApiKeys() => NavigationRequested?.Invoke("ApiKeys");
}
