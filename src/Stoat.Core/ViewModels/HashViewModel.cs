using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Stoat.Services.Enums;
using Stoat.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Stoat.Core.ViewModels;

public partial class HashViewModel : ViewModelBase
{
    private readonly IHashService _hashService;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _outputHash = string.Empty;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private HashAlgorithmType _selectedAlgorithm = HashAlgorithmType.SHA256;

    [ObservableProperty]
    private bool _isTextMode = true;

    [ObservableProperty]
    private bool _isFileMode;

    [ObservableProperty]
    private string? _selectedFilePath;

    [ObservableProperty]
    private string? _selectedFileName;

    [ObservableProperty]
    private bool _isHashing;

    [ObservableProperty]
    private double _hashProgress;

    [ObservableProperty]
    private bool _canHash;

    // Verification
    [ObservableProperty]
    private string _verifyHash = string.Empty;

    [ObservableProperty]
    private bool _isVerifying;

    public ObservableCollection<HashAlgorithmType> AlgorithmOptions { get; } = new(Enum.GetValues<HashAlgorithmType>());

    public HashViewModel(IHashService hashService, ILocalizationService localizationService)
    {
        _hashService = hashService;
        _localizationService = localizationService;
    }

    partial void OnInputTextChanged(string value) => UpdateCanHash();
    partial void OnSelectedFilePathChanged(string? value) => UpdateCanHash();
    partial void OnIsFileModeChanged(bool value) => UpdateCanHash();

    private void UpdateCanHash()
    {
        if (IsFileMode)
            CanHash = !string.IsNullOrWhiteSpace(SelectedFilePath);
        else
            CanHash = !string.IsNullOrWhiteSpace(InputText);
    }

    [RelayCommand]
    private async Task ComputeHashAsync()
    {
        if (IsFileMode)
            await HashFileInternalAsync();
        else
            HashText();
    }

    private void HashText()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            IsError = true;
            StatusMessage = _localizationService.Format("Hash.Error.NoText");
            return;
        }

        try
        {
            OutputHash = _hashService.HashText(InputText, SelectedAlgorithm);
            IsError = false;
            StatusMessage = _localizationService.Format("Hash.Success.Computed", SelectedAlgorithm);
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = ex.Message;
        }
    }

    private async Task HashFileInternalAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedFilePath))
        {
            IsError = true;
            StatusMessage = _localizationService.Format("Hash.Error.NoFile");
            return;
        }

        IsHashing = true;
        HashProgress = 0;

        try
        {
            var progress = new Progress<double>(p => HashProgress = p);
            OutputHash = await _hashService.HashFileAsync(SelectedFilePath, SelectedAlgorithm, progress);
            IsError = false;
            StatusMessage = _localizationService.Format("Hash.Success.Computed", SelectedAlgorithm);
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = ex.Message;
        }
        finally
        {
            IsHashing = false;
        }
    }

    [RelayCommand]
    private void VerifyHashRelay()
    {
        if (string.IsNullOrWhiteSpace(OutputHash) || string.IsNullOrWhiteSpace(VerifyHash))
        {
            IsError = true;
            StatusMessage = _localizationService.Format("Hash.Verify.Error.Missing");
            return;
        }

        if (string.Equals(OutputHash, VerifyHash.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            IsError = false;
            StatusMessage = _localizationService.Format("Hash.Verify.Success.Match");
        }
        else
        {
            IsError = true;
            StatusMessage = _localizationService.Format("Hash.Verify.Error.Mismatch");
        }
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (!string.IsNullOrEmpty(OutputHash))
        {
            ClipboardCopyRequested?.Invoke(OutputHash);
            StatusMessage = _localizationService.Format("Hash.Success.Copied");
            IsError = false;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        InputText = string.Empty;
        OutputHash = string.Empty;
        VerifyHash = string.Empty;
        StatusMessage = null;
        IsError = false;
        SelectedFilePath = null;
        SelectedFileName = null;
    }

    [RelayCommand]
    private void SetTextMode()
    {
        IsTextMode = true;
        IsFileMode = false;
    }

    [RelayCommand]
    private void SetFileMode()
    {
        IsTextMode = false;
        IsFileMode = true;
    }

    [RelayCommand]
    private void SelectFile()
    {
        SelectFileRequested?.Invoke();
    }

    public void SetSelectedFile(string? filePath)
    {
        SelectedFilePath = filePath;
        SelectedFileName = filePath != null ? System.IO.Path.GetFileName(filePath) : null;
    }

    public event Action<string>? ClipboardCopyRequested;
    public event Action? SelectFileRequested;
}
