using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Stoat.Services.Interfaces;
using Stoat.Services.Models;
using Stoat.Services.Enums;
using System.Collections.ObjectModel;

namespace Stoat.Core.ViewModels;

public partial class EncryptionViewModel : ViewModelBase
{
    private readonly IEncryptionService _encryptionService;
    private readonly IProfileService _profileService;
    private readonly IConfirmationService _confirmationService;
    private readonly ILocalizationService _localizationService;
    private bool _isLoadingSettings;
    private bool _isApplyingPreset;

    // Mode toggle
    [ObservableProperty]
    private bool _isEncryptMode = true;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private string _activeProfileName = string.Empty;

    [ObservableProperty]
    private bool _hasActiveProfile;

    [ObservableProperty]
    private bool _canProcess;

    [ObservableProperty]
    private bool _isTextMode = true;

    [ObservableProperty]
    private bool _isFileMode;

    [ObservableProperty]
    private string? _selectedFilePath;

    [ObservableProperty]
    private string? _selectedFileName;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string? _outputFilePath;

    [ObservableProperty]
    private bool _showAdvancedSettings;

    // Preset selection
    [ObservableProperty]
    private EncryptionPresetItem? _selectedPresetItem;

    // Save preset modal
    [ObservableProperty]
    private bool _isShowingSavePresetModal;

    [ObservableProperty]
    private string _newPresetName = string.Empty;

    [ObservableProperty]
    private string? _savePresetError;

    // Preset modification tracking
    [ObservableProperty]
    private bool _isPresetModified;

    public ObservableCollection<EncryptionPresetItem> PresetItems { get; } = new();
    public bool IsPresetSelected => SelectedPresetItem != null && !SelectedPresetItem.IsCustom;

    // Encryption settings
    [ObservableProperty]
    private SymmetricCipher _selectedCipher = SymmetricCipher.AES;

    [ObservableProperty]
    private int? _selectedKeySizeBits = 256;

    [ObservableProperty]
    private CipherBlockMode? _selectedMode = CipherBlockMode.CBC;

    [ObservableProperty]
    private KeyDerivationFunction _selectedKdf = KeyDerivationFunction.PBKDF2;

    [ObservableProperty]
    private KdfHashAlgorithm _selectedKdfHash = KdfHashAlgorithm.SHA256;

    [ObservableProperty]
    private int _selectedIterations = 100000;

    // Argon2 parameters
    [ObservableProperty]
    private int _argon2MemoryKB = 65536;

    [ObservableProperty]
    private int _argon2Parallelism = 4;

    [ObservableProperty]
    private int _argon2Iterations = 3;

    // Scrypt parameters
    [ObservableProperty]
    private int _scryptN = 16384;

    [ObservableProperty]
    private int _scryptR = 8;

    [ObservableProperty]
    private int _scryptP = 1;

    // Dynamic collections
    public ObservableCollection<SymmetricCipher> CipherOptions { get; } = new(Enum.GetValues<SymmetricCipher>());
    public ObservableCollection<int> KeySizeOptions { get; } = new();
    public ObservableCollection<CipherBlockMode> ModeOptions { get; } = new();
    public ObservableCollection<KeyDerivationFunction> KdfOptions { get; } = new(Enum.GetValues<KeyDerivationFunction>());
    public ObservableCollection<KdfHashAlgorithm> KdfHashOptions { get; } = new(Enum.GetValues<KdfHashAlgorithm>());

    // Visibility helpers
    public bool ShowModeSelector => SelectedCipher != SymmetricCipher.ChaCha20;
    public bool ShowPbkdf2Options => SelectedKdf == KeyDerivationFunction.PBKDF2;
    public bool ShowArgon2Options => SelectedKdf == KeyDerivationFunction.Argon2id;
    public bool ShowScryptOptions => SelectedKdf == KeyDerivationFunction.Scrypt;
    public bool ShowHkdfOptions => SelectedKdf == KeyDerivationFunction.HKDF;

    // Computed properties for adaptive texts
    public string InputLabel => IsEncryptMode ? _localizationService.Format("Encryption.Label.TextToEncrypt") : _localizationService.Format("Encryption.Label.EncryptedText");
    public string OutputLabel => IsEncryptMode ? _localizationService.Format("Encryption.Label.EncryptedOutput") : _localizationService.Format("Encryption.Label.DecryptedOutput");
    public string InputWatermark => IsEncryptMode ? _localizationService.Format("Encryption.Placeholder.ToEncrypt") : _localizationService.Format("Encryption.Placeholder.ToDecrypt");
    public string ActionButtonText => IsEncryptMode ? _localizationService.Format("Encryption.Button.Encrypt") : _localizationService.Format("Encryption.Button.Decrypt");
    public string ProcessingText => IsEncryptMode ? _localizationService.Format("Encryption.Status.Encrypting") : _localizationService.Format("Encryption.Status.Decrypting");
    public string SettingsTitle => IsEncryptMode ? _localizationService.Format("Encryption.Settings.TitleEncrypt") : _localizationService.Format("Encryption.Settings.TitleDecrypt");
    public string DropZoneText => IsEncryptMode ? _localizationService.Format("Encryption.FileDropZone.DragText") : _localizationService.Format("Encryption.FileDropZone.DragEncryptedText");
    public string DropZoneHint => IsEncryptMode ? string.Empty : string.Empty;

    // Settings visibility: always in encrypt mode, only in text mode in decrypt mode
    public bool ShowSettings => IsEncryptMode || IsTextMode;

    // 64-bit block cipher warning: only in encrypt mode
    public bool Show64BitWarning => IsEncryptMode && CipherMetadata.Is64BitBlockCipher(SelectedCipher);

    public EncryptionViewModel(
        IEncryptionService encryptionService,
        IProfileService profileService,
        IConfirmationService confirmationService,
        ILocalizationService localizationService)
    {
        _encryptionService = encryptionService;
        _profileService = profileService;
        _confirmationService = confirmationService;
        _localizationService = localizationService;

        _profileService.ActiveProfileChanged += OnActiveProfileChanged;
        UpdateActiveProfileDisplay();
        UpdateDynamicOptions();
    }

    public override void OnNavigatedTo()
    {
        UpdateActiveProfileDisplay();
        _ = LoadSettingsAndPresetsAsync();
    }

    private void OnActiveProfileChanged(object? sender, ProfileChangedEventArgs e)
    {
        UpdateActiveProfileDisplay();
        _ = LoadSettingsAndPresetsAsync();
    }

    private async Task LoadSettingsAndPresetsAsync()
    {
        await LoadSettingsFromActiveProfileAsync();
        await RefreshPresetItemsAsync();
    }

    private void UpdateActiveProfileDisplay()
    {
        var profile = _profileService.ActiveProfile;
        HasActiveProfile = profile != null;
        ActiveProfileName = profile?.Name ?? _localizationService.Format("Encryption.Profile.NoProfileSelected");
        UpdateCanProcess();
    }

    partial void OnInputTextChanged(string value) => UpdateCanProcess();
    partial void OnSelectedFilePathChanged(string? value) => UpdateCanProcess();
    partial void OnIsFileModeChanged(bool value)
    {
        UpdateCanProcess();
        NotifyModeChanged();
    }

    partial void OnIsEncryptModeChanged(bool value)
    {
        NotifyModeChanged();
        UpdateCanProcess();
    }

    private void NotifyModeChanged()
    {
        OnPropertyChanged(nameof(InputLabel));
        OnPropertyChanged(nameof(OutputLabel));
        OnPropertyChanged(nameof(InputWatermark));
        OnPropertyChanged(nameof(ActionButtonText));
        OnPropertyChanged(nameof(ProcessingText));
        OnPropertyChanged(nameof(SettingsTitle));
        OnPropertyChanged(nameof(DropZoneText));
        OnPropertyChanged(nameof(DropZoneHint));
        OnPropertyChanged(nameof(ShowSettings));
        OnPropertyChanged(nameof(Show64BitWarning));
    }

    partial void OnSelectedCipherChanged(SymmetricCipher value)
    {
        UpdateDynamicOptions();
        OnPropertyChanged(nameof(ShowModeSelector));
        OnPropertyChanged(nameof(Show64BitWarning));
        OnSettingChanged();
    }

    partial void OnSelectedKeySizeBitsChanged(int? value) => OnSettingChanged();
    partial void OnSelectedModeChanged(CipherBlockMode? value) => OnSettingChanged();

    partial void OnSelectedKdfChanged(KeyDerivationFunction value)
    {
        OnPropertyChanged(nameof(ShowPbkdf2Options));
        OnPropertyChanged(nameof(ShowArgon2Options));
        OnPropertyChanged(nameof(ShowScryptOptions));
        OnPropertyChanged(nameof(ShowHkdfOptions));
        OnSettingChanged();
    }

    partial void OnSelectedKdfHashChanged(KdfHashAlgorithm value) => OnSettingChanged();
    partial void OnSelectedIterationsChanged(int value) => OnSettingChanged();
    partial void OnArgon2MemoryKBChanged(int value) => OnSettingChanged();
    partial void OnArgon2ParallelismChanged(int value) => OnSettingChanged();
    partial void OnArgon2IterationsChanged(int value) => OnSettingChanged();
    partial void OnScryptNChanged(int value) => OnSettingChanged();
    partial void OnScryptRChanged(int value) => OnSettingChanged();
    partial void OnScryptPChanged(int value) => OnSettingChanged();

    private void UpdateDynamicOptions()
    {
        var keySizes = CipherMetadata.GetSupportedKeySizes(SelectedCipher);
        var targetKeySize = SelectedKeySizeBits.HasValue && keySizes.Contains(SelectedKeySizeBits.Value)
            ? SelectedKeySizeBits.Value
            : CipherMetadata.GetDefaultKeySizeBits(SelectedCipher);

        KeySizeOptions.Clear();
        foreach (var size in keySizes)
            KeySizeOptions.Add(size);
        SelectedKeySizeBits = targetKeySize;

        var modes = CipherMetadata.GetSupportedModes(SelectedCipher);
        var targetMode = SelectedMode.HasValue && modes.Contains(SelectedMode.Value)
            ? SelectedMode.Value
            : (modes.Length > 0 ? modes[0] : (CipherBlockMode?)null);

        ModeOptions.Clear();
        foreach (var mode in modes)
            ModeOptions.Add(mode);
        SelectedMode = targetMode;
    }

    private void UpdateCanProcess()
    {
        if (IsFileMode)
            CanProcess = HasActiveProfile && !string.IsNullOrWhiteSpace(SelectedFilePath);
        else
            CanProcess = HasActiveProfile && !string.IsNullOrWhiteSpace(InputText);
    }

    private async Task LoadSettingsFromActiveProfileAsync()
    {
        var profile = _profileService.ActiveProfile;
        if (profile == null) return;

        var settings = await _profileService.GetEncryptionSettingsAsync(profile.Id);
        ApplySettings(settings ?? EncryptionSettings.Default);
    }

    private void ApplySettings(EncryptionSettings settings)
    {
        _isLoadingSettings = true;
        try
        {
            SelectedCipher = settings.Cipher;
            SelectedKeySizeBits = settings.KeySizeBits;
            SelectedMode = settings.Mode;
            SelectedKdf = settings.Kdf;
            SelectedKdfHash = settings.KdfHash;
            SelectedIterations = settings.KdfIterations;
            Argon2MemoryKB = settings.Argon2MemoryKB;
            Argon2Parallelism = settings.Argon2Parallelism;
            Argon2Iterations = settings.Argon2Iterations;
            ScryptN = settings.ScryptN;
            ScryptR = settings.ScryptR;
            ScryptP = settings.ScryptP;
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    partial void OnSelectedPresetItemChanged(EncryptionPresetItem? value)
    {
        OnPropertyChanged(nameof(IsPresetSelected));
        IsPresetModified = false;
        if (value == null || _isLoadingSettings) return;

        var profile = _profileService.ActiveProfile;
        if (profile == null) return;

        if (value.IsCustom)
        {
            _ = _profileService.SetSelectedPresetIdAsync(profile.Id, null);
            return;
        }

        // Apply the preset's settings
        _isApplyingPreset = true;
        try
        {
            ApplySettings(value.Preset!.Settings);
            _ = SaveCurrentSettingsAsync();
            _ = _profileService.SetSelectedPresetIdAsync(profile.Id, value.Id);
        }
        finally
        {
            _isApplyingPreset = false;
        }
    }

    private void OnSettingChanged()
    {
        if (_isLoadingSettings) return;

        if (!_isApplyingPreset)
        {
            // If a preset is selected, mark it as modified; otherwise stay on "Personalizzato"
            if (SelectedPresetItem != null && !SelectedPresetItem.IsCustom)
                IsPresetModified = true;
            else
                SelectCustomPreset();
        }

        _ = SaveCurrentSettingsAsync();
    }

    private async Task SaveCurrentSettingsAsync()
    {
        var profile = _profileService.ActiveProfile;
        if (profile == null) return;

        var settings = GetCurrentSettings();
        await _profileService.SaveEncryptionSettingsAsync(profile.Id, settings);
    }

    private EncryptionSettings GetCurrentSettings() => new()
    {
        Cipher = SelectedCipher,
        KeySizeBits = SelectedKeySizeBits ?? 256,
        Mode = SelectedMode ?? CipherBlockMode.CBC,
        Kdf = SelectedKdf,
        KdfHash = SelectedKdfHash,
        KdfIterations = SelectedIterations,
        Argon2MemoryKB = Argon2MemoryKB,
        Argon2Parallelism = Argon2Parallelism,
        Argon2Iterations = Argon2Iterations,
        ScryptN = ScryptN,
        ScryptR = ScryptR,
        ScryptP = ScryptP
    };

    [RelayCommand]
    private void SetEncryptMode()
    {
        IsEncryptMode = true;
        Clear();
    }

    [RelayCommand]
    private void SetDecryptMode()
    {
        IsEncryptMode = false;
        Clear();
    }

    [RelayCommand]
    private async Task ProcessAsync()
    {
        if (!HasActiveProfile)
        {
            IsError = true;
            StatusMessage = IsEncryptMode
                ? _localizationService.Format("Encryption.Error.NoProfileToEncrypt")
                : _localizationService.Format("Encryption.Error.NoProfileToDecrypt");
            return;
        }

        if (IsFileMode)
            await ProcessFileAsync();
        else
            ProcessText();
    }

    private void ProcessText()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            IsError = true;
            StatusMessage = IsEncryptMode
                ? _localizationService.Format("Encryption.Error.NoTextToEncrypt")
                : _localizationService.Format("Encryption.Error.NoTextToDecrypt");
            return;
        }

        var settings = GetCurrentSettings();

        if (IsEncryptMode)
        {
            if (_encryptionService.TryEncrypt(InputText, out var encrypted, out var error, settings))
            {
                OutputText = encrypted ?? string.Empty;
                IsError = false;
                StatusMessage = _localizationService.Format("Encryption.Success.TextEncrypted", ActiveProfileName);
            }
            else
            {
                IsError = true;
                StatusMessage = error ?? _localizationService.Format("Encryption.Error.EncryptionFailed");
            }
        }
        else
        {
            if (_encryptionService.TryDecrypt(InputText, out var decrypted, out var error, settings))
            {
                OutputText = decrypted ?? string.Empty;
                IsError = false;
                StatusMessage = _localizationService.Format("Encryption.Success.TextDecrypted", ActiveProfileName);
            }
            else
            {
                IsError = true;
                StatusMessage = error ?? _localizationService.Format("Encryption.Error.DecryptionFailed");
            }
        }
    }

    private async Task ProcessFileAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedFilePath))
        {
            IsError = true;
            StatusMessage = IsEncryptMode
                ? _localizationService.Format("Encryption.Error.NoFileToEncrypt")
                : _localizationService.Format("Encryption.Error.NoFileToDecrypt");
            return;
        }

        IsProcessing = true;
        Progress = 0;
        OutputFilePath = null;

        var progress = new Progress<double>(p => Progress = p);

        if (IsEncryptMode)
        {
            var settings = GetCurrentSettings();
            var (success, resultPath, error) = await _encryptionService.TryEncryptFileAsync(
                SelectedFilePath, settings: settings, progress: progress);

            IsProcessing = false;

            if (success)
            {
                OutputFilePath = resultPath;
                IsError = false;
                StatusMessage = _localizationService.Format("Encryption.Success.FileEncrypted", System.IO.Path.GetFileName(resultPath) ?? string.Empty);
            }
            else
            {
                IsError = true;
                StatusMessage = error ?? _localizationService.Format("Encryption.Error.FileEncryptionFailed");
            }
        }
        else
        {
            var (success, resultPath, error) = await _encryptionService.TryDecryptFileAsync(
                SelectedFilePath, progress: progress);

            IsProcessing = false;

            if (success)
            {
                OutputFilePath = resultPath;
                IsError = false;
                StatusMessage = _localizationService.Format("Encryption.Success.FileDecrypted", System.IO.Path.GetFileName(resultPath) ?? string.Empty);
            }
            else
            {
                IsError = true;
                StatusMessage = error ?? _localizationService.Format("Encryption.Error.FileDecryptionFailed");
            }
        }
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (!string.IsNullOrEmpty(OutputText))
        {
            ClipboardCopyRequested?.Invoke(OutputText);
            StatusMessage = IsEncryptMode
                ? _localizationService.Format("Encryption.Success.CopiedEncrypted")
                : _localizationService.Format("Encryption.Success.CopiedDecrypted");
            IsError = false;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        InputText = string.Empty;
        OutputText = string.Empty;
        StatusMessage = null;
        IsError = false;
        SelectedFilePath = null;
        SelectedFileName = null;
        OutputFilePath = null;
        Progress = 0;
    }

    [RelayCommand]
    private void SwapFields()
    {
        (InputText, OutputText) = (OutputText, InputText);
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

    [RelayCommand]
    private void ToggleAdvancedSettings()
    {
        ShowAdvancedSettings = !ShowAdvancedSettings;
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        SelectedCipher = SymmetricCipher.AES;
        SelectedKeySizeBits = 256;
        SelectedMode = CipherBlockMode.CBC;
        SelectedKdf = KeyDerivationFunction.PBKDF2;
        SelectedKdfHash = KdfHashAlgorithm.SHA256;
        SelectedIterations = 100000;
        Argon2MemoryKB = 65536;
        Argon2Parallelism = 4;
        Argon2Iterations = 3;
        ScryptN = 16384;
        ScryptR = 8;
        ScryptP = 1;
    }

    public void SetSelectedFile(string? filePath)
    {
        SelectedFilePath = filePath;
        SelectedFileName = filePath != null ? System.IO.Path.GetFileName(filePath) : null;
    }

    // --- Preset commands ---

    [RelayCommand]
    private void ShowSavePresetModal()
    {
        NewPresetName = string.Empty;
        SavePresetError = null;
        IsShowingSavePresetModal = true;
    }

    [RelayCommand]
    private void CancelSavePreset()
    {
        IsShowingSavePresetModal = false;
    }

    [RelayCommand]
    private async Task SavePresetAsync()
    {
        var profile = _profileService.ActiveProfile;
        if (profile == null) return;

        var name = NewPresetName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            SavePresetError = _localizationService.Format("Encryption.Preset.Error.NoName");
            return;
        }

        // Check for duplicate name
        var existingPresets = await _profileService.GetPresetsAsync(profile.Id);
        if (existingPresets.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            SavePresetError = _localizationService.Format("Encryption.Preset.Error.DuplicateName");
            return;
        }

        var settings = GetCurrentSettings();
        var preset = await _profileService.SavePresetAsync(profile.Id, name, settings);

        IsShowingSavePresetModal = false;

        await RefreshPresetItemsAsync();

        // Select the newly created preset
        var newItem = PresetItems.FirstOrDefault(p => p.Id == preset.Id);
        if (newItem != null)
        {
            _isLoadingSettings = true;
            SelectedPresetItem = newItem;
            _isLoadingSettings = false;
            OnPropertyChanged(nameof(IsPresetSelected));
            await _profileService.SetSelectedPresetIdAsync(profile.Id, preset.Id);
        }
    }

    [RelayCommand]
    private async Task UpdatePresetAsync()
    {
        if (SelectedPresetItem == null || SelectedPresetItem.IsCustom || !IsPresetModified) return;

        var profile = _profileService.ActiveProfile;
        if (profile == null) return;

        var settings = GetCurrentSettings();
        await _profileService.UpdatePresetAsync(profile.Id, SelectedPresetItem.Id!.Value, settings);

        // Update the local Preset object so future comparisons are correct
        SelectedPresetItem.Preset!.Settings = settings.Clone();
        IsPresetModified = false;
    }

    [RelayCommand]
    private async Task DeletePresetAsync()
    {
        if (SelectedPresetItem == null || SelectedPresetItem.IsCustom) return;

        var profile = _profileService.ActiveProfile;
        if (profile == null) return;

        var confirmed = await _confirmationService.ConfirmAsync(
            _localizationService.Format("Encryption.Preset.Dialog.DeleteTitle"),
            _localizationService.Format("Encryption.Preset.Dialog.DeleteConfirm", SelectedPresetItem.Name));

        if (!confirmed) return;

        await _profileService.DeletePresetAsync(profile.Id, SelectedPresetItem.Id!.Value);
        await RefreshPresetItemsAsync();
        SelectCustomPreset();
    }

    private async Task RefreshPresetItemsAsync()
    {
        var profile = _profileService.ActiveProfile;
        if (profile == null) return;

        var presets = await _profileService.GetPresetsAsync(profile.Id);
        var selectedPresetId = await _profileService.GetSelectedPresetIdAsync(profile.Id);

        _isLoadingSettings = true;
        try
        {
            PresetItems.Clear();

            // First item is always "Personalizzato"
            PresetItems.Add(EncryptionPresetItem.Custom(_localizationService.Format("Encryption.Preset.Custom")));

            foreach (var preset in presets)
                PresetItems.Add(EncryptionPresetItem.FromPreset(preset));

            // Restore selection from profile data
            if (selectedPresetId.HasValue)
            {
                var restored = PresetItems.FirstOrDefault(p => p.Id == selectedPresetId.Value);
                SelectedPresetItem = restored ?? PresetItems[0];
            }
            else
            {
                SelectedPresetItem = PresetItems[0];
            }
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    private void SelectCustomPreset()
    {
        IsPresetModified = false;
        if (PresetItems.Count > 0 && (SelectedPresetItem == null || !SelectedPresetItem.IsCustom))
        {
            _isLoadingSettings = true;
            SelectedPresetItem = PresetItems[0]; // "Personalizzato"
            _isLoadingSettings = false;
            OnPropertyChanged(nameof(IsPresetSelected));

            var profile = _profileService.ActiveProfile;
            if (profile != null)
                _ = _profileService.SetSelectedPresetIdAsync(profile.Id, null);
        }
    }

    public event Action<string>? ClipboardCopyRequested;
    public event Action? SelectFileRequested;
}

public class EncryptionPresetItem
{
    public Guid? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsCustom { get; init; }
    public EncryptionPreset? Preset { get; init; }

    public static EncryptionPresetItem Custom(string customName) => new()
    {
        Id = null,
        Name = customName,
        IsCustom = true,
        Preset = null
    };

    public static EncryptionPresetItem FromPreset(EncryptionPreset preset) => new()
    {
        Id = preset.Id,
        Name = preset.Name,
        IsCustom = false,
        Preset = preset
    };

    public override string ToString() => Name;
}
