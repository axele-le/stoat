using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Stoat.Services.Exceptions;
using Stoat.Services.Interfaces;
using Stoat.Services.Models;

namespace Stoat.Core.ViewModels;

public class AutoLockSettingsEventArgs : EventArgs
{
    public bool Enabled { get; set; }
    public int TimeoutMinutes { get; set; }
}

public class ClipboardSettingsEventArgs : EventArgs
{
    public bool Enabled { get; set; }
    public int TimeoutSeconds { get; set; }
}

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IProfileService _profileService;
    private readonly IClientSettingsService _clientSettingsService;
    private readonly ILockService _lockService;
    private readonly IConfirmationService _confirmationService;
    private readonly ILocalizationService _localizationService;
    private readonly IToastService _toastService;

    private bool _isLoadingSettings; // prevents auto-save during load
    private CancellationTokenSource? _saveDebounce;

    [ObservableProperty]
    private ObservableCollection<ProfileItemViewModel> _profiles = new();

    // Create profile
    [ObservableProperty]
    private string _newProfileName = string.Empty;

    // Client settings - Aspetto
    [ObservableProperty]
    private int _selectedThemeIndex = 0; // 0=Scuro, 1=Chiaro, 2=Sistema

    [ObservableProperty]
    private int _selectedLanguageIndex = 0; // 0=Italiano, 1=English

    // Client settings - Sicurezza & Comportamento
    [ObservableProperty]
    private bool _autoLockEnabled = false;

    [ObservableProperty]
    private int _lockTimeoutIndex = 1; // 0=1min, 1=5min, 2=15min, 3=30min

    [ObservableProperty]
    private bool _clearClipboardEnabled = true;

    [ObservableProperty]
    private int _clipboardTimeoutIndex = 1; // 0=10s, 1=30s, 2=60s, 3=120s

    [ObservableProperty]
    private bool _confirmOperationsEnabled = true;

    // Master password state
    [ObservableProperty]
    private bool _hasMasterPassword;

    public bool CanEnableAutoLock => HasMasterPassword;

    partial void OnHasMasterPasswordChanged(bool value)
    {
        OnPropertyChanged(nameof(CanEnableAutoLock));
        if (!value && AutoLockEnabled)
        {
            AutoLockEnabled = false;
        }
    }

    // Master password setup/change fields
    [ObservableProperty]
    private string _masterPassword = string.Empty;

    [ObservableProperty]
    private string _masterPasswordConfirm = string.Empty;

    [ObservableProperty]
    private int _masterPasswordStrength;

    [ObservableProperty]
    private string _currentMasterPassword = string.Empty;

    [ObservableProperty]
    private bool _isSettingMasterPassword;

    [ObservableProperty]
    private bool _isChangingMasterPassword;

    public bool MasterStrength1 => MasterPasswordStrength >= 1;
    public bool MasterStrength2 => MasterPasswordStrength >= 2;
    public bool MasterStrength3 => MasterPasswordStrength >= 3;
    public bool MasterStrength4 => MasterPasswordStrength >= 4;
    public bool IsMasterStrengthWeak => MasterPasswordStrength == 1;
    public bool IsMasterStrengthMedium => MasterPasswordStrength == 2;
    public bool IsMasterStrengthStrong => MasterPasswordStrength == 3;
    public bool IsMasterStrengthVeryStrong => MasterPasswordStrength == 4;

    partial void OnMasterPasswordChanged(string value)
    {
        MasterPasswordStrength = CalculatePasswordStrength(value);
    }

    partial void OnMasterPasswordStrengthChanged(int value)
    {
        OnPropertyChanged(nameof(MasterStrength1));
        OnPropertyChanged(nameof(MasterStrength2));
        OnPropertyChanged(nameof(MasterStrength3));
        OnPropertyChanged(nameof(MasterStrength4));
        OnPropertyChanged(nameof(IsMasterStrengthWeak));
        OnPropertyChanged(nameof(IsMasterStrengthMedium));
        OnPropertyChanged(nameof(IsMasterStrengthStrong));
        OnPropertyChanged(nameof(IsMasterStrengthVeryStrong));
    }

    // Recovery key modal
    [ObservableProperty]
    private string _recoveryKeyDisplay = string.Empty;

    [ObservableProperty]
    private bool _isRecoveryKeyModalVisible;

    public event EventHandler? CopyRecoveryKeyRequested;

    // Event to notify MainWindow about auto-lock settings changes
    public event EventHandler<AutoLockSettingsEventArgs>? AutoLockSettingsChanged;

    // Event to notify about clipboard settings changes
    public event EventHandler<ClipboardSettingsEventArgs>? ClipboardSettingsChanged;

    // Event to notify about theme changes
    public event EventHandler<int>? ThemeChanged;

    // Tab selection: 0=Profili, 1=Client, 2=Esporta/Importa
    [ObservableProperty]
    private int _activeTabIndex = 0;

    // Keep IsProfiliTabActive for backward compatibility with existing UI bindings
    public bool IsProfiliTabActive => ActiveTabIndex == 0;
    public bool IsClientTabActive => ActiveTabIndex == 1;
    public bool IsExportImportTabActive => ActiveTabIndex == 2;

    partial void OnActiveTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsProfiliTabActive));
        OnPropertyChanged(nameof(IsClientTabActive));
        OnPropertyChanged(nameof(IsExportImportTabActive));

        // Clear export/import passwords when leaving the tab
        if (value != 2)
        {
            ExportPassword = string.Empty;
            ExportPasswordConfirm = string.Empty;
            ImportPassword = string.Empty;
        }
    }

    // Export/Import sub-tab
    [ObservableProperty]
    private bool _isExportSubTabActive = true;

    // Export state
    [ObservableProperty]
    private string _exportPassword = string.Empty;

    [ObservableProperty]
    private string _exportPasswordConfirm = string.Empty;

    [ObservableProperty]
    private int _exportPasswordStrength; // 0-4

    [ObservableProperty]
    private bool _isExporting;

    public bool CanExport => !IsExporting
        && !string.IsNullOrEmpty(ExportPassword)
        && ExportPassword == ExportPasswordConfirm
        && ExportPassword.Length >= 8
        && Profiles.Any(p => p.IsSelectedForExport);

    // Computed properties for strength bar segments and labels
    public bool ExportStrength1 => ExportPasswordStrength >= 1;
    public bool ExportStrength2 => ExportPasswordStrength >= 2;
    public bool ExportStrength3 => ExportPasswordStrength >= 3;
    public bool ExportStrength4 => ExportPasswordStrength >= 4;
    public bool IsExportStrengthWeak => ExportPasswordStrength == 1;
    public bool IsExportStrengthMedium => ExportPasswordStrength == 2;
    public bool IsExportStrengthStrong => ExportPasswordStrength == 3;
    public bool IsExportStrengthVeryStrong => ExportPasswordStrength == 4;

    partial void OnExportPasswordChanged(string value)
    {
        ExportPasswordStrength = CalculatePasswordStrength(value);
        OnPropertyChanged(nameof(CanExport));
    }

    partial void OnExportPasswordStrengthChanged(int value)
    {
        OnPropertyChanged(nameof(ExportStrength1));
        OnPropertyChanged(nameof(ExportStrength2));
        OnPropertyChanged(nameof(ExportStrength3));
        OnPropertyChanged(nameof(ExportStrength4));
        OnPropertyChanged(nameof(IsExportStrengthWeak));
        OnPropertyChanged(nameof(IsExportStrengthMedium));
        OnPropertyChanged(nameof(IsExportStrengthStrong));
        OnPropertyChanged(nameof(IsExportStrengthVeryStrong));
    }

    partial void OnExportPasswordConfirmChanged(string value)
    {
        OnPropertyChanged(nameof(CanExport));
    }

    // Import state
    [ObservableProperty]
    private string _importPassword = string.Empty;

    [ObservableProperty]
    private bool _isImporting;

    [ObservableProperty]
    private string? _importFileName;

    [ObservableProperty]
    private string? _importPreviewInfo;

    public bool HasImportFile => _pendingImportPackage != null;

    private ExportPackage? _pendingImportPackage;

    // Conflict resolution: 0=Rinomina, 1=Sostituisci, 2=Salta
    [ObservableProperty]
    private int _conflictResolutionIndex = 0;

    public bool IsConflictRename => ConflictResolutionIndex == 0;
    public bool IsConflictReplace => ConflictResolutionIndex == 1;
    public bool IsConflictSkip => ConflictResolutionIndex == 2;

    partial void OnConflictResolutionIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsConflictRename));
        OnPropertyChanged(nameof(IsConflictReplace));
        OnPropertyChanged(nameof(IsConflictSkip));
    }

    // Events for file picker (handled in code-behind)
    public event EventHandler<string>? SaveExportFileRequested;
    public event EventHandler? OpenImportFileRequested;

    public SettingsViewModel(
        IProfileService profileService,
        IClientSettingsService clientSettingsService,
        ILockService lockService,
        IConfirmationService confirmationService,
        ILocalizationService localizationService,
        IToastService toastService)
    {
        _profileService = profileService;
        _clientSettingsService = clientSettingsService;
        _lockService = lockService;
        _confirmationService = confirmationService;
        _localizationService = localizationService;
        _toastService = toastService;
        _profileService.ActiveProfileChanged += OnActiveProfileChanged;
        InitializeLocalization(localizationService, OnLanguageChanged);
    }

    private void OnLanguageChanged()
    {
        // Toast messages are already dispatched, no property to refresh
    }

    public override async void OnNavigatedTo()
    {
        await LoadProfilesAsync();
        await LoadClientSettingsAsync();
    }

    private void OnActiveProfileChanged(object? sender, ProfileChangedEventArgs e)
    {
        foreach (var profile in Profiles)
        {
            profile.IsActive = profile.Id == e.NewProfile?.Id;
        }
    }

    [RelayCommand]
    private async Task LoadProfilesAsync()
    {
        try
        {
            var profiles = await _profileService.GetAllProfilesAsync();
            var activeId = _profileService.ActiveProfile?.Id;

            Profiles.Clear();
            foreach (var profile in profiles)
            {
                Profiles.Add(new ProfileItemViewModel
                {
                    Id = profile.Id,
                    Name = profile.Name,
                    Description = profile.Description,
                    CreatedAt = profile.CreatedAtUtc.ToLocalTime(),
                    LastUsedAt = profile.LastUsedAtUtc?.ToLocalTime(),
                    IsActive = profile.Id == activeId,
                    IsExpanded = false,
                    Salt = "************************",
                    Secret = "************************"
                });
            }

        }
        catch (Exception ex)
        {
            _toastService.Error(_localizationService.Format("Common.Error.WithMessage", ex.Message));
        }
    }

    [RelayCommand]
    private async Task CreateProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(NewProfileName))
        {
            _toastService.Warning(_localizationService.Format("Settings.Profile.Create.EmptyNameError"));
            return;
        }

        try
        {
            var profile = await _profileService.CreateProfileAsync(
                NewProfileName.Trim(),
                null,
                setAsActive: Profiles.Count == 0);

            Profiles.Add(new ProfileItemViewModel
            {
                Id = profile.Id,
                Name = profile.Name,
                Description = profile.Description,
                CreatedAt = profile.CreatedAtUtc.ToLocalTime(),
                IsActive = Profiles.Count == 0,
                Salt = "************************",
                Secret = "************************"
            });

            NewProfileName = string.Empty;

            _toastService.Success(_localizationService.Format("Settings.Profile.Create.Success", profile.Name));
        }
        catch (Exception ex)
        {
            _toastService.Error(_localizationService.Format("Common.Error.WithMessage", ex.Message));
        }
    }

    [RelayCommand]
    private async Task SetActiveProfileAsync(ProfileItemViewModel? profile)
    {
        if (profile == null || profile.IsActive) return;

        try
        {
            await _profileService.SetActiveProfileAsync(profile.Id);

            foreach (var p in Profiles)
            {
                p.IsActive = p.Id == profile.Id;
            }

            _toastService.Success(_localizationService.Format("Settings.Profile.SetActive.Success", profile.Name));
        }
        catch (Exception ex)
        {
            _toastService.Error(_localizationService.Format("Common.Error.WithMessage", ex.Message));
        }
    }

    [RelayCommand]
    private async Task DeleteProfileAsync(ProfileItemViewModel? profile)
    {
        if (profile == null) return;

        if (Profiles.Count <= 1)
        {
            _toastService.Error(_localizationService.Format("Settings.Profile.Error.CannotDeleteOnly"));
            return;
        }

        if (!await _confirmationService.ConfirmAsync(_localizationService.Format("Settings.Profile.Dialog.DeleteTitle"), _localizationService.Format("Settings.Profile.Dialog.DeleteConfirm", profile.Name)))
            return;

        try
        {
            await _profileService.DeleteProfileAsync(profile.Id);
            Profiles.Remove(profile);

            _toastService.Success(_localizationService.Format("Settings.Profile.Delete.Success", profile.Name));
        }
        catch (Exception ex)
        {
            _toastService.Error(_localizationService.Format("Common.Error.WithMessage", ex.Message));
        }
    }

    [RelayCommand]
    private async Task ToggleExpandAsync(ProfileItemViewModel? profile)
    {
        if (profile == null) return;

        // Collapse all others
        foreach (var p in Profiles)
        {
            if (p != profile)
            {
                p.IsExpanded = false;
            }
        }

        profile.IsExpanded = !profile.IsExpanded;

        // Load credentials when expanding
        if (profile.IsExpanded)
        {
            try
            {
                var credentials = await _profileService.GetProfileCredentialsAsync(profile.Id);
                if (credentials.HasValue)
                {
                    profile.Salt = credentials.Value.salt;
                    profile.Secret = credentials.Value.secret;
                }
            }
            catch (Exception ex)
            {
                _toastService.Error(_localizationService.Format("Settings.Credentials.Error.Loading", ex.Message));
            }
        }
    }

    [RelayCommand]
    private void ToggleSaltVisibility(ProfileItemViewModel? profile)
    {
        if (profile != null)
        {
            profile.ShowSalt = !profile.ShowSalt;
        }
    }

    [RelayCommand]
    private void ToggleSecretVisibility(ProfileItemViewModel? profile)
    {
        if (profile != null)
        {
            profile.ShowSecret = !profile.ShowSecret;
        }
    }

    [RelayCommand]
    private async Task RegenerateSaltAsync(ProfileItemViewModel? profile)
    {
        if (profile == null) return;

        if (!await _confirmationService.ConfirmAsync(_localizationService.Format("Settings.Credentials.Dialog.RegenerateSaltTitle"), _localizationService.Format("Settings.Credentials.Dialog.RegenerateSaltConfirm")))
            return;

        try
        {
            var newSalt = Guid.NewGuid().ToString("N")[..16];
            await _profileService.UpdateProfileCredentialsAsync(profile.Id, null, newSalt);
            profile.Salt = newSalt;

            _toastService.Success(_localizationService.Format("Settings.Credentials.Success.SaltRegenerated"));
        }
        catch (Exception ex)
        {
            _toastService.Error(_localizationService.Format("Common.Error.WithMessage", ex.Message));
        }
    }

    [RelayCommand]
    private async Task RegenerateSecretAsync(ProfileItemViewModel? profile)
    {
        if (profile == null) return;

        if (!await _confirmationService.ConfirmAsync(_localizationService.Format("Settings.Credentials.Dialog.RegenerateSecretTitle"), _localizationService.Format("Settings.Credentials.Dialog.RegenerateSecretConfirm")))
            return;

        try
        {
            var newSecret = Guid.NewGuid().ToString("N");
            await _profileService.UpdateProfileCredentialsAsync(profile.Id, newSecret, null);
            profile.Secret = newSecret;

            _toastService.Success(_localizationService.Format("Settings.Credentials.Success.SecretRegenerated"));
        }
        catch (Exception ex)
        {
            _toastService.Error(_localizationService.Format("Common.Error.WithMessage", ex.Message));
        }
    }

    [RelayCommand]
    private async Task SaveSaltAsync(ProfileItemViewModel? profile)
    {
        if (profile == null) return;

        if (string.IsNullOrWhiteSpace(profile.Salt))
        {
            _toastService.Error(_localizationService.Format("Settings.Credentials.Error.SaltEmpty"));
            return;
        }

        try
        {
            await _profileService.UpdateProfileCredentialsAsync(profile.Id, null, profile.Salt);
            _toastService.Success(_localizationService.Format("Settings.Credentials.Success.SaltSaved"));
        }
        catch (Exception ex)
        {
            _toastService.Error(_localizationService.Format("Common.Error.WithMessage", ex.Message));
        }
    }

    [RelayCommand]
    private async Task SaveSecretAsync(ProfileItemViewModel? profile)
    {
        if (profile == null) return;

        if (string.IsNullOrWhiteSpace(profile.Secret))
        {
            _toastService.Error(_localizationService.Format("Settings.Credentials.Error.SecretEmpty"));
            return;
        }

        try
        {
            await _profileService.UpdateProfileCredentialsAsync(profile.Id, profile.Secret, null);
            _toastService.Success(_localizationService.Format("Settings.Credentials.Success.SecretSaved"));
        }
        catch (Exception ex)
        {
            _toastService.Error(_localizationService.Format("Common.Error.WithMessage", ex.Message));
        }
    }

    [RelayCommand]
    private void StartEditing(ProfileItemViewModel? profile)
    {
        if (profile == null) return;

        // Cancel any other editing in progress
        foreach (var p in Profiles)
        {
            if (p != profile && p.IsEditing)
            {
                p.IsEditing = false;
            }
        }

        profile.EditingName = profile.Name;
        profile.IsEditing = true;
    }

    [RelayCommand]
    private async Task ConfirmEditAsync(ProfileItemViewModel? profile)
    {
        if (profile == null) return;

        var newName = profile.EditingName?.Trim();
        if (string.IsNullOrWhiteSpace(newName))
        {
            _toastService.Error(_localizationService.Format("Settings.Profile.Error.NameEmpty"));
            profile.IsEditing = false;
            return;
        }

        if (newName == profile.Name)
        {
            profile.IsEditing = false;
            return;
        }

        try
        {
            await _profileService.UpdateProfileAsync(profile.Id, newName, profile.Description);
            
            profile.Name = newName;
            profile.IsEditing = false;

            _toastService.Success(_localizationService.Format("Settings.Profile.Success.Renamed", newName));
        }
        catch (Exception ex)
        {
            _toastService.Error(_localizationService.Format("Common.Error.WithMessage", ex.Message));
            profile.IsEditing = false;
        }
    }

    [RelayCommand]
    private void CancelEdit(ProfileItemViewModel? profile)
    {
        if (profile == null) return;
        profile.IsEditing = false;
        profile.EditingName = profile.Name;
    }

    // --- Export/Import Commands ---

    [RelayCommand]
    private void SelectAllForExport()
    {
        foreach (var p in Profiles)
            p.IsSelectedForExport = true;
        OnPropertyChanged(nameof(CanExport));
    }

    [RelayCommand]
    private void DeselectAllForExport()
    {
        foreach (var p in Profiles)
            p.IsSelectedForExport = false;
        OnPropertyChanged(nameof(CanExport));
    }

    [RelayCommand]
    private void ToggleExportSelection(ProfileItemViewModel? profile)
    {
        if (profile != null)
        {
            profile.IsSelectedForExport = !profile.IsSelectedForExport;
            OnPropertyChanged(nameof(CanExport));
        }
    }

    [RelayCommand]
    private async Task ExportProfilesAsync()
    {
        if (!CanExport) return;

        IsExporting = true;
        try
        {
            var selectedIds = Profiles
                .Where(p => p.IsSelectedForExport)
                .Select(p => p.Id);

            var package = await _profileService.ExportProfilesAsync(selectedIds, ExportPassword);
            var json = JsonSerializer.Serialize(package, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            SaveExportFileRequested?.Invoke(this, json);
        }
        catch (Exception ex)
        {
            _toastService.Error(_localizationService.Format("Common.Error.WithMessage", ex.Message));
        }
        finally
        {
            IsExporting = false;
        }
    }

    public void SetExportFileSaveResult(bool success, string message)
    {
        if (success)
        {
            ExportPassword = string.Empty;
            ExportPasswordConfirm = string.Empty;
            foreach (var p in Profiles)
                p.IsSelectedForExport = false;
            OnPropertyChanged(nameof(CanExport));
        }

        if (!success)
        {
            _toastService.Error(message);
        }
        else
        {
            _toastService.Success(message);
        }
    }

    [RelayCommand]
    private void BrowseImportFile()
    {
        OpenImportFileRequested?.Invoke(this, EventArgs.Empty);
    }

    public void SetImportPackageData(string json, string fileName)
    {
        try
        {
            _pendingImportPackage = JsonSerializer.Deserialize<ExportPackage>(json,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            if (_pendingImportPackage == null)
            {
                _toastService.Error(_localizationService.Format("Settings.Import.Error.InvalidFile"));
                return;
            }

            ImportFileName = fileName;
            var format = _pendingImportPackage.FormatVersion >= 2 ? "AES-256-GCM" : "AES-256-CBC";
            var date = _pendingImportPackage.ExportedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            var machine = _pendingImportPackage.SourceMachineName ?? _localizationService.Format("Settings.Import.Preview.MachineUnknown");
            ImportPreviewInfo = _localizationService.Format("Settings.Import.Preview.InfoFormat", date, machine, format);

            OnPropertyChanged(nameof(HasImportFile));
        }
        catch (JsonException)
        {
            _toastService.Error(_localizationService.Format("Settings.Import.Error.NotExportFile"));

            _pendingImportPackage = null;
            ImportFileName = null;
            ImportPreviewInfo = null;
            
            OnPropertyChanged(nameof(HasImportFile));
        }
    }

    [RelayCommand]
    private async Task ImportProfilesAsync()
    {
        if (_pendingImportPackage == null || string.IsNullOrEmpty(ImportPassword)) return;

        IsImporting = true;
        try
        {
            var resolution = ConflictResolutionIndex switch
            {
                1 => ImportConflictResolution.Replace,
                2 => ImportConflictResolution.Skip,
                _ => ImportConflictResolution.Rename
            };

            var result = await _profileService.ImportProfilesAsync(
                _pendingImportPackage, ImportPassword, resolution);

            var message = _localizationService.Format("Settings.Import.Success.Completed", result.ImportedCount);
            if (result.SkippedCount > 0)
                message += $", " + _localizationService.Format("Settings.Import.Result.Skipped", result.SkippedCount);
            if (result.ReplacedCount > 0)
                message += $", " + _localizationService.Format("Settings.Import.Result.Replaced", result.ReplacedCount);

            _toastService.Success(message);

            // Reset import state
            _pendingImportPackage = null;
            ImportPassword = string.Empty;
            ImportFileName = null;
            ImportPreviewInfo = null;
            OnPropertyChanged(nameof(HasImportFile));

            // Refresh profile list
            await LoadProfilesAsync();

            _toastService.Success(message);
        }
        catch (InvalidExportPasswordException)
        {
            _toastService.Error(_localizationService.Format("Settings.Import.Error.BadPasswordOrCorrupted"));
        }
        catch (Exception ex)
        {
            _toastService.Error(_localizationService.Format("Settings.Import.Error.ImportFailed", ex.Message));
        }
        finally
        {
            IsImporting = false;
        }
    }

    // --- Client Settings Persistence ---

    private async Task LoadClientSettingsAsync()
    {
        _isLoadingSettings = true;
        try
        {
            var settings = await _clientSettingsService.LoadAsync();
            SelectedThemeIndex = settings.SelectedThemeIndex;
            SelectedLanguageIndex = settings.SelectedLanguageIndex;
            AutoLockEnabled = settings.AutoLockEnabled;
            LockTimeoutIndex = settings.LockTimeoutIndex;
            ClearClipboardEnabled = settings.ClearClipboardEnabled;
            ClipboardTimeoutIndex = settings.ClipboardTimeoutIndex;
            ConfirmOperationsEnabled = settings.ConfirmOperationsEnabled;
            HasMasterPassword = _lockService.HasMasterPassword;
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    partial void OnSelectedThemeIndexChanged(int value)
    {
        ThemeChanged?.Invoke(this, value);
        DebounceSaveClientSettings();
    }
    partial void OnSelectedLanguageIndexChanged(int value)
    {
        DebounceSaveClientSettings();
        if (!_isLoadingSettings && Loc != null)
        {
            var languages = Loc.AvailableLanguages;
            if (value >= 0 && value < languages.Count)
                Loc.SetLanguage(languages[value].Code);
        }
    }
    partial void OnClearClipboardEnabledChanged(bool value)
    {
        DebounceSaveClientSettings();
        FireClipboardSettingsChanged();
    }

    partial void OnClipboardTimeoutIndexChanged(int value)
    {
        DebounceSaveClientSettings();
        FireClipboardSettingsChanged();
    }
    partial void OnConfirmOperationsEnabledChanged(bool value) => DebounceSaveClientSettings();

    partial void OnAutoLockEnabledChanged(bool value)
    {
        DebounceSaveClientSettings();
        FireAutoLockSettingsChanged();
    }

    partial void OnLockTimeoutIndexChanged(int value)
    {
        DebounceSaveClientSettings();
        FireAutoLockSettingsChanged();
    }

    private void FireClipboardSettingsChanged()
    {
        if (_isLoadingSettings) return;

        var timeoutSeconds = ClipboardTimeoutIndex switch
        {
            0 => 10,
            1 => 30,
            2 => 60,
            3 => 120,
            _ => 30
        };

        ClipboardSettingsChanged?.Invoke(this, new ClipboardSettingsEventArgs
        {
            Enabled = ClearClipboardEnabled,
            TimeoutSeconds = timeoutSeconds
        });
    }

    private void FireAutoLockSettingsChanged()
    {
        if (_isLoadingSettings) return;

        var timeoutMinutes = LockTimeoutIndex switch
        {
            0 => 1,
            1 => 5,
            2 => 15,
            3 => 30,
            _ => 5
        };

        AutoLockSettingsChanged?.Invoke(this, new AutoLockSettingsEventArgs
        {
            Enabled = AutoLockEnabled,
            TimeoutMinutes = timeoutMinutes
        });
    }

    private void DebounceSaveClientSettings()
    {
        if (_isLoadingSettings) return;

        _saveDebounce?.Cancel();
        _saveDebounce = new CancellationTokenSource();
        var token = _saveDebounce.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, token);
                if (token.IsCancellationRequested) return;

                var settings = new ClientSettings
                {
                    SelectedThemeIndex = SelectedThemeIndex,
                    SelectedLanguageIndex = SelectedLanguageIndex,
                    AutoLockEnabled = AutoLockEnabled,
                    LockTimeoutIndex = LockTimeoutIndex,
                    ClearClipboardEnabled = ClearClipboardEnabled,
                    ClipboardTimeoutIndex = ClipboardTimeoutIndex,
                    ConfirmOperationsEnabled = ConfirmOperationsEnabled
                };

                await _clientSettingsService.SaveAsync(settings, token);
            }
            catch (OperationCanceledException)
            {
                // Debounce cancelled, ignore
            }
        });
    }

    // --- Master Password Commands ---

    [RelayCommand]
    private void StartSetupMasterPassword()
    {
        MasterPassword = string.Empty;
        MasterPasswordConfirm = string.Empty;
        IsSettingMasterPassword = true;
        IsChangingMasterPassword = false;
    }

    [RelayCommand]
    private void StartChangeMasterPassword()
    {
        CurrentMasterPassword = string.Empty;
        MasterPassword = string.Empty;
        MasterPasswordConfirm = string.Empty;
        IsChangingMasterPassword = true;
        IsSettingMasterPassword = false;
    }

    [RelayCommand]
    private void CancelMasterPasswordAction()
    {
        IsSettingMasterPassword = false;
        IsChangingMasterPassword = false;
        MasterPassword = string.Empty;
        MasterPasswordConfirm = string.Empty;
        CurrentMasterPassword = string.Empty;
    }

    [RelayCommand]
    private void DismissRecoveryKeyModal()
    {
        IsRecoveryKeyModalVisible = false;
        RecoveryKeyDisplay = string.Empty;
    }

    [RelayCommand]
    private void CopyRecoveryKey()
    {
        CopyRecoveryKeyRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task ConfirmSetupMasterPasswordAsync()
    {
        if (string.IsNullOrEmpty(MasterPassword) || MasterPassword.Length < 8)
        {
            _toastService.Error(_localizationService.Format("Settings.MasterPassword.Error.TooShort"));
            return;
        }

        if (MasterPassword != MasterPasswordConfirm)
        {
            _toastService.Error(_localizationService.Format("Settings.MasterPassword.Error.Mismatch"));
            return;
        }

        try
        {
            var recoveryKey = await _lockService.SetupMasterPasswordWithRecoveryAsync(MasterPassword);
            HasMasterPassword = true;
            IsSettingMasterPassword = false;
            MasterPassword = string.Empty;
            MasterPasswordConfirm = string.Empty;

            _toastService.Success(_localizationService.Format("Settings.MasterPassword.Success.Setup"));

            RecoveryKeyDisplay = recoveryKey;
            IsRecoveryKeyModalVisible = true;
        }
        catch (Exception ex)
        {
            _toastService.Error(_localizationService.Format("Common.Error.WithMessage", ex.Message));
        }
    }

    [RelayCommand]
    private async Task ConfirmChangeMasterPasswordAsync()
    {
        if (string.IsNullOrEmpty(CurrentMasterPassword))
        {
            _toastService.Error(_localizationService.Format("Settings.MasterPassword.Error.CurrentRequired"));
            return;
        }

        if (string.IsNullOrEmpty(MasterPassword) || MasterPassword.Length < 8)
        {
            _toastService.Error(_localizationService.Format("Settings.MasterPassword.Error.NewTooShort"));
            return;
        }

        if (MasterPassword != MasterPasswordConfirm)
        {
            _toastService.Error(_localizationService.Format("Settings.MasterPassword.Error.NewMismatch"));
            return;
        }

        try
        {
            var recoveryKey = await _lockService.ChangeMasterPasswordWithRecoveryAsync(CurrentMasterPassword, MasterPassword);
            if (recoveryKey == null)
            {
                _toastService.Error(_localizationService.Format("Settings.MasterPassword.Error.CurrentIncorrect"));
                return;
            }

            IsChangingMasterPassword = false;
            CurrentMasterPassword = string.Empty;
            MasterPassword = string.Empty;
            MasterPasswordConfirm = string.Empty;

            _toastService.Success(_localizationService.Format("Settings.MasterPassword.Success.Changed"));

            RecoveryKeyDisplay = recoveryKey;
            IsRecoveryKeyModalVisible = true;
        }
        catch (Exception ex)
        {
            _toastService.Error(_localizationService.Format("Common.Error.WithMessage", ex.Message));
        }
    }

    [RelayCommand]
    private async Task RemoveMasterPasswordAsync()
    {
        if (string.IsNullOrEmpty(CurrentMasterPassword))
        {
            // Show the change form so user can enter current password
            CurrentMasterPassword = string.Empty;
            IsChangingMasterPassword = true;
            IsSettingMasterPassword = false;

            _toastService.Error(_localizationService.Format("Settings.MasterPassword.Error.RemoveRequiresPassword"));
            return;
        }

        try
        {
            var success = await _lockService.RemoveMasterPasswordAsync(CurrentMasterPassword);
            if (!success)
            {
                _toastService.Error(_localizationService.Format("Settings.MasterPassword.Error.IncorrectPassword"));
                return;
            }

            HasMasterPassword = false;
            AutoLockEnabled = false;
            IsChangingMasterPassword = false;
            CurrentMasterPassword = string.Empty;
            MasterPassword = string.Empty;
            MasterPasswordConfirm = string.Empty;

            _toastService.Success(_localizationService.Format("Settings.MasterPassword.Success.Removed"));
            
            AutoLockSettingsChanged?.Invoke(this, new AutoLockSettingsEventArgs
            {
                Enabled = false,
                TimeoutMinutes = 0
            });
        }
        catch (Exception ex)
        {
            _toastService.Error(_localizationService.Format("Common.Error.WithMessage", ex.Message));
        }
    }

    private static int CalculatePasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password)) return 0;

        var score = 0;
        if (password.Length >= 8) score++;
        if (password.Length >= 12) score++;
        if (password.Any(char.IsUpper) && password.Any(char.IsLower)) score++;
        if (password.Any(char.IsDigit) && password.Any(c => !char.IsLetterOrDigit(c))) score++;

        return score;
    }
}
