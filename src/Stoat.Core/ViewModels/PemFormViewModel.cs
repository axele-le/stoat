using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Stoat.Services.Interfaces;

namespace Stoat.Core.ViewModels;

public partial class PemFormViewModel : ObservableObject
{
    private readonly IPemStorageService _pemStorageService;
    private readonly IPemService _pemService;
    private readonly ILocalizationService _localizationService;
    private readonly IToastService _toastService;

    [ObservableProperty]
    private bool _isGenerateMode = true;

    [ObservableProperty]
    private bool _isImportMode;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateKeyPairCommand))]
    private string _newKeyName = string.Empty;

    [ObservableProperty]
    private int _selectedKeySize = 4096;

    public string SelectedAlgorithm => $"RSA-{SelectedKeySize}";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateKeyPairCommand))]
    private bool _isGenerating;

    [ObservableProperty]
    private string? _selectedFileName;

    public int[] AvailableKeySizes { get; } = [2048, 3072, 4096];

    public event Action? CloseRequested;
    public event Action? GenerationSucceeded;
    public event Action? ImportFileRequested;

    public PemFormViewModel(IPemStorageService pemStorageService, IPemService pemService, ILocalizationService localizationService, IToastService toastService)
    {
        _pemStorageService = pemStorageService;
        _pemService = pemService;
        _localizationService = localizationService;
        _toastService = toastService;
    }

    partial void OnSelectedKeySizeChanged(int value)
    {
        OnPropertyChanged(nameof(SelectedAlgorithm));
    }

    [RelayCommand]
    private void SetGenerateMode()
    {
        IsGenerateMode = true;
        IsImportMode = false;
    }

    [RelayCommand]
    private void SetImportMode()
    {
        IsGenerateMode = false;
        IsImportMode = true;
    }

    [RelayCommand(CanExecute = nameof(CanGenerateKeyPair))]
    private async Task GenerateKeyPairAsync()
    {
        if (string.IsNullOrWhiteSpace(NewKeyName))
        {
            _toastService.Warning(_localizationService.Format("Pem.Form.Error.NoName"));
            return;
        }

        IsGenerating = true;

        try
        {
            await _pemStorageService.CreateKeyPairAsync(NewKeyName.Trim(), SelectedKeySize);
            _toastService.Success(_localizationService.Format("Pem.Form.Success.Generated", SelectedKeySize));

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

    private bool CanGenerateKeyPair() => !IsGenerating && !string.IsNullOrWhiteSpace(NewKeyName);

    [RelayCommand]
    private void ImportKey()
    {
        ImportFileRequested?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke();
    }

    public async Task HandleImportedFileAsync(string fileName, string content)
    {
        try
        {
            var isPrivateKey = content.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase);
            var isPublicKey = content.Contains("PUBLIC KEY", StringComparison.OrdinalIgnoreCase);

            var name = Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrWhiteSpace(name))
            {
                name = _localizationService.Format("Pem.Import.DefaultName");
            }

            if (isPrivateKey)
            {
                var publicKey = _pemService.ExtractPublicKeyFromPrivate(content);
                if (publicKey == null)
                {
                    _toastService.Error(_localizationService.Format("Pem.Import.Error.CannotExtractPublic"));
                    return;
                }

                await _pemStorageService.ImportKeyPairAsync(name, content, publicKey);
                SelectedFileName = fileName;
                _toastService.Success(_localizationService.Format("Pem.Import.Success.PrivateKey", name));
                GenerationSucceeded?.Invoke();
            }
            else if (isPublicKey)
            {
                await _pemStorageService.ImportPublicKeyOnlyAsync(name, content);
                SelectedFileName = fileName;
                _toastService.Success(_localizationService.Format("Pem.Import.Success.PublicKey", name));
                GenerationSucceeded?.Invoke();
            }
            else
            {
                _toastService.Error(_localizationService.Format("Pem.Import.Error.InvalidFormat"));
            }
        }
        catch (Exception ex)
        {
            _toastService.Error(_localizationService.Format("Pem.Import.Error.Failed", ex.Message));
        }
    }

    public void ClearForm()
    {
        NewKeyName = string.Empty;
        SelectedKeySize = 4096;
        SelectedFileName = null;
    }

}
