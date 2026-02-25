using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Stoat.Services.Interfaces;
using Stoat.Services.Models;

namespace Stoat.Core.ViewModels;

/// <summary>
/// ViewModel for the CSR import form.
/// Handles importing CSR files and optionally private key files.
/// </summary>
public partial class CsrImportFormViewModel : ObservableObject
{
    private readonly ICsrImportService _importService;
    private readonly ICsrStorageService _storageService;
    private readonly ILocalizationService _localizationService;

    // Import mode
    [ObservableProperty]
    private bool _importCsrOnly = true;

    [ObservableProperty]
    private bool _importCsrWithKey;

    // CSR file
    [ObservableProperty]
    private string? _csrFilePath;

    [ObservableProperty]
    private string? _csrFileName;

    [ObservableProperty]
    private bool _hasCsrFile;

    // Private key file
    [ObservableProperty]
    private string? _keyFilePath;

    [ObservableProperty]
    private string? _keyFileName;

    [ObservableProperty]
    private bool _hasKeyFile;

    [ObservableProperty]
    private string? _keyPassword;

    // Parsed preview
    [ObservableProperty]
    private bool _showPreview;

    [ObservableProperty]
    private string? _previewCommonName;

    [ObservableProperty]
    private string? _previewAlgorithm;

    [ObservableProperty]
    private string? _previewSubjectDN;

    [ObservableProperty]
    private int _previewSanCount;

    [ObservableProperty]
    private bool _previewHasPrivateKey;

    [ObservableProperty]
    private bool _keyPairMatch;

    [ObservableProperty]
    private string? _keyPairMatchError;

    // Import name
    [ObservableProperty]
    private string _importName = string.Empty;

    // Status
    [ObservableProperty]
    private bool _isImporting;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private bool _canImport;

    // Parsed results (internal)
    private CsrImportResult? _csrResult;
    private CsrImportResult? _keyResult;

    // Events
    public event Action? CloseRequested;
    public event Action? ImportSucceeded;
    public event Action? SelectCsrFileRequested;
    public event Action? SelectKeyFileRequested;

    public CsrImportFormViewModel(ICsrImportService importService, ICsrStorageService storageService, ILocalizationService localizationService)
    {
        _importService = importService;
        _storageService = storageService;
        _localizationService = localizationService;
    }

    partial void OnImportCsrOnlyChanged(bool value)
    {
        if (value)
        {
            ImportCsrWithKey = false;
            ClearKeyFile();
            ValidateAndPreview();
        }
    }

    partial void OnImportCsrWithKeyChanged(bool value)
    {
        if (value)
        {
            ImportCsrOnly = false;
            ValidateAndPreview();
        }
    }

    partial void OnImportNameChanged(string value) => UpdateCanImport();

    [RelayCommand]
    private void SelectCsrFile()
    {
        SelectCsrFileRequested?.Invoke();
    }

    [RelayCommand]
    private void SelectKeyFile()
    {
        SelectKeyFileRequested?.Invoke();
    }

    public async Task SetCsrFileAsync(string filePath)
    {
        CsrFilePath = filePath;
        CsrFileName = System.IO.Path.GetFileName(filePath);
        HasCsrFile = true;

        // Parse immediately
        _csrResult = await _importService.ParseCsrFileAsync(filePath);

        if (!_csrResult.Success)
        {
            StatusMessage = _csrResult.Error;
            IsError = true;
            ShowPreview = false;
            HasCsrFile = false;
            CsrFileName = null;
            _csrResult = null;
        }
        else
        {
            StatusMessage = null;
            IsError = false;

            // Auto-fill name from CN if empty
            if (string.IsNullOrEmpty(ImportName) && _csrResult.ParsedData != null)
            {
                ImportName = _csrResult.ParsedData.CommonName;
            }
        }

        ValidateAndPreview();
    }

    public async Task SetKeyFileAsync(string filePath)
    {
        KeyFilePath = filePath;
        KeyFileName = System.IO.Path.GetFileName(filePath);
        HasKeyFile = true;

        // Parse immediately
        _keyResult = await _importService.ParsePrivateKeyFileAsync(filePath, KeyPassword);

        if (!_keyResult.Success)
        {
            StatusMessage = _keyResult.Error;
            IsError = true;
            HasKeyFile = false;
            KeyFileName = null;
            _keyResult = null;
        }
        else
        {
            StatusMessage = null;
            IsError = false;
        }

        ValidateAndPreview();
    }

    private void ClearKeyFile()
    {
        KeyFilePath = null;
        KeyFileName = null;
        HasKeyFile = false;
        KeyPassword = null;
        _keyResult = null;
        KeyPairMatch = false;
        KeyPairMatchError = null;
    }

    private void ValidateAndPreview()
    {
        if (_csrResult is { Success: true })
        {
            ShowPreview = true;
            PreviewCommonName = _csrResult.ParsedData?.CommonName ?? _localizationService?.Format("CsrImport.Preview.Unavailable");
            PreviewAlgorithm = _csrResult.AlgorithmDisplay ?? _localizationService?.Format("CsrImport.Preview.Unknown");
            PreviewSubjectDN = _csrResult.SubjectDN ?? _localizationService?.Format("CsrImport.Preview.Unavailable");
            PreviewSanCount = _csrResult.SanCount;

            // Check key pair match if both files are provided
            if (ImportCsrWithKey && _keyResult is { Success: true } && _csrResult.CsrPem != null && _keyResult.PrivateKeyPem != null)
            {
                var match = _importService.ValidateKeyPairMatch(_csrResult.CsrPem, _keyResult.PrivateKeyPem);
                KeyPairMatch = match;
                PreviewHasPrivateKey = match;
                KeyPairMatchError = match ? null : _localizationService?.Format("CsrImport.Error.KeyPairMismatch");
            }
            else if (ImportCsrOnly)
            {
                PreviewHasPrivateKey = false;
                KeyPairMatch = false;
                KeyPairMatchError = null;
            }
            else
            {
                PreviewHasPrivateKey = false;
                KeyPairMatch = false;
                KeyPairMatchError = null;
            }
        }
        else
        {
            ShowPreview = false;
        }

        UpdateCanImport();
    }

    private void UpdateCanImport()
    {
        if (string.IsNullOrWhiteSpace(ImportName))
        {
            CanImport = false;
            return;
        }

        if (_csrResult is not { Success: true })
        {
            CanImport = false;
            return;
        }

        if (ImportCsrWithKey)
        {
            CanImport = _keyResult is { Success: true } && KeyPairMatch;
            return;
        }

        CanImport = true;
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        if (_csrResult is not { Success: true } || !CanImport) return;

        IsImporting = true;
        StatusMessage = null;

        try
        {
            string? privateKeyPem = null;
            if (ImportCsrWithKey && _keyResult is { Success: true })
            {
                privateKeyPem = _keyResult.PrivateKeyPem;
            }

            await _storageService.ImportCsrAsync(
                name: ImportName.Trim(),
                csrPem: _csrResult.CsrPem!,
                privateKeyPem: privateKeyPem,
                publicKeyPem: _csrResult.PublicKeyPem!,
                keyAlgorithm: _csrResult.DetectedKeyAlgorithm!.Value,
                signatureAlgorithm: _csrResult.DetectedSignatureAlgorithm,
                parsedData: _csrResult.ParsedData!,
                sanCount: _csrResult.SanCount,
                sanList: _csrResult.SanList,
                sourceFileName: _csrResult.SourceFileName);

            IsImporting = false;
            ImportSucceeded?.Invoke();
        }
        catch (Exception ex)
        {
            IsImporting = false;
            StatusMessage = _localizationService?.Format("CsrImport.Error.ImportFailed", ex.Message);
            IsError = true;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private void SetImportCsrOnly()
    {
        ImportCsrOnly = true;
    }

    [RelayCommand]
    private void SetImportCsrWithKey()
    {
        ImportCsrWithKey = true;
    }
}
