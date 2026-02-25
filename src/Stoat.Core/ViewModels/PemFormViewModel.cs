using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Stoat.Services.Interfaces;

namespace Stoat.Core.ViewModels;

public partial class PemFormViewModel : ObservableObject
{
    private readonly IPemStorageService _pemStorageService;
    private readonly IPemService _pemService;
    private readonly ILocalizationService _localizationService;

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
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private string? _selectedFileName;

    public int[] AvailableKeySizes { get; } = [2048, 3072, 4096];

    public event Action? CloseRequested;
    public event Action? GenerationSucceeded;
    public event Action? ImportFileRequested;

    public PemFormViewModel(IPemStorageService pemStorageService, IPemService pemService, ILocalizationService localizationService)
    {
        _pemStorageService = pemStorageService;
        _pemService = pemService;
        _localizationService = localizationService;
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
        ClearStatus();
    }

    [RelayCommand]
    private void SetImportMode()
    {
        IsGenerateMode = false;
        IsImportMode = true;
        ClearStatus();
    }

    [RelayCommand(CanExecute = nameof(CanGenerateKeyPair))]
    private async Task GenerateKeyPairAsync()
    {
        if (string.IsNullOrWhiteSpace(NewKeyName))
        {
            SetStatus(_localizationService.Format("Pem.Form.Error.NoName"), isError: true);
            return;
        }

        IsGenerating = true;
        IsError = false;
        StatusMessage = _localizationService.Format("Pem.Form.Status.Generating");

        try
        {
            await _pemStorageService.CreateKeyPairAsync(NewKeyName.Trim(), SelectedKeySize);
            SetStatus(_localizationService.Format("Pem.Form.Success.Generated", SelectedKeySize));

            GenerationSucceeded?.Invoke();
            ClearForm();
        }
        catch (Exception ex)
        {
            SetStatus(_localizationService.Format("Common.Error.WithMessage", ex.Message), isError: true);
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
                    SetStatus(_localizationService.Format("Pem.Import.Error.CannotExtractPublic"), isError: true);
                    return;
                }

                await _pemStorageService.ImportKeyPairAsync(name, content, publicKey);
                SelectedFileName = fileName;
                SetStatus(_localizationService.Format("Pem.Import.Success.PrivateKey", name));
                GenerationSucceeded?.Invoke();
            }
            else if (isPublicKey)
            {
                await _pemStorageService.ImportPublicKeyOnlyAsync(name, content);
                SelectedFileName = fileName;
                SetStatus(_localizationService.Format("Pem.Import.Success.PublicKey", name));
                GenerationSucceeded?.Invoke();
            }
            else
            {
                SetStatus(_localizationService.Format("Pem.Import.Error.InvalidFormat"), isError: true);
            }
        }
        catch (Exception ex)
        {
            SetStatus(_localizationService.Format("Pem.Import.Error.Failed", ex.Message), isError: true);
        }
    }

    public void ClearForm()
    {
        NewKeyName = string.Empty;
        SelectedKeySize = 4096;
        SelectedFileName = null;
    }

    private void SetStatus(string message, bool isError = false)
    {
        StatusMessage = message;
        IsError = isError;
    }

    private void ClearStatus()
    {
        StatusMessage = null;
        IsError = false;
    }
}
