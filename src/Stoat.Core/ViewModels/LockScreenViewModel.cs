using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Stoat.Services.Interfaces;

namespace Stoat.Core.ViewModels;

public partial class LockScreenViewModel : ViewModelBase
{
    private readonly ILockService _lockService;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isUnlocking;

    [ObservableProperty]
    private bool _isRecoveryMode;

    [ObservableProperty]
    private string _recoveryKey = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _newPasswordConfirm = string.Empty;

    [ObservableProperty]
    private int _newPasswordStrength;

    public bool NewStrength1 => NewPasswordStrength >= 1;
    public bool NewStrength2 => NewPasswordStrength >= 2;
    public bool NewStrength3 => NewPasswordStrength >= 3;
    public bool NewStrength4 => NewPasswordStrength >= 4;
    public bool IsNewStrengthWeak => NewPasswordStrength == 1;
    public bool IsNewStrengthMedium => NewPasswordStrength == 2;
    public bool IsNewStrengthStrong => NewPasswordStrength == 3;
    public bool IsNewStrengthVeryStrong => NewPasswordStrength == 4;

    partial void OnNewPasswordChanged(string value)
    {
        NewPasswordStrength = CalculatePasswordStrength(value);
    }

    partial void OnNewPasswordStrengthChanged(int value)
    {
        OnPropertyChanged(nameof(NewStrength1));
        OnPropertyChanged(nameof(NewStrength2));
        OnPropertyChanged(nameof(NewStrength3));
        OnPropertyChanged(nameof(NewStrength4));
        OnPropertyChanged(nameof(IsNewStrengthWeak));
        OnPropertyChanged(nameof(IsNewStrengthMedium));
        OnPropertyChanged(nameof(IsNewStrengthStrong));
        OnPropertyChanged(nameof(IsNewStrengthVeryStrong));
    }

    [ObservableProperty]
    private string _recoveryKeyDisplay = string.Empty;

    [ObservableProperty]
    private bool _isRecoveryKeyModalVisible;

    private string? _pendingUnlockPassword;

    public event EventHandler? CopyRecoveryKeyRequested;

    public LockScreenViewModel(ILockService lockService, ILocalizationService localizationService)
    {
        _localizationService = localizationService;
        _lockService = lockService;
    }

    [RelayCommand]
    private async Task UnlockAsync()
    {
        if (string.IsNullOrEmpty(Password))
        {
            ErrorMessage = _localizationService.Format("Lock.Unlock.Error.NoPassword");
            return;
        }

        IsUnlocking = true;
        ErrorMessage = null;

        try
        {
            var success = await _lockService.UnlockAsync(Password);
            if (!success)
            {
                ErrorMessage = _localizationService.Format("Lock.Unlock.Error.IncorrectPassword");
                Password = string.Empty;
            }
            else
            {
                Password = string.Empty;
                ErrorMessage = null;
            }
        }
        catch (Exception)
        {
            ErrorMessage = _localizationService.Format("Lock.Unlock.Error.UnlockFailed");
        }
        finally
        {
            IsUnlocking = false;
        }
    }

    [RelayCommand]
    private void StartRecovery()
    {
        IsRecoveryMode = true;
        ErrorMessage = null;
        RecoveryKey = string.Empty;
        NewPassword = string.Empty;
        NewPasswordConfirm = string.Empty;
    }

    [RelayCommand]
    private void CancelRecovery()
    {
        IsRecoveryMode = false;
        ErrorMessage = null;
        RecoveryKey = string.Empty;
        NewPassword = string.Empty;
        NewPasswordConfirm = string.Empty;
    }

    [RelayCommand]
    private async Task ResetPasswordAsync()
    {
        if (string.IsNullOrEmpty(RecoveryKey))
        {
            ErrorMessage = _localizationService.Format("Lock.Recovery.Error.NoKey");
            return;
        }

        if (string.IsNullOrEmpty(NewPassword) || NewPassword.Length < 8)
        {
            ErrorMessage = _localizationService.Format("Lock.Recovery.Error.PasswordTooShort");
            return;
        }

        if (NewPassword != NewPasswordConfirm)
        {
            ErrorMessage = _localizationService.Format("Lock.Recovery.Error.PasswordMismatch");
            return;
        }

        IsUnlocking = true;
        ErrorMessage = null;

        try
        {
            var newRecoveryKey = await _lockService.ResetPasswordWithRecoveryKeyAsync(RecoveryKey, NewPassword);
            if (newRecoveryKey == null)
            {
                ErrorMessage = _localizationService.Format("Lock.Recovery.Error.InvalidKey");
                return;
            }

            _pendingUnlockPassword = NewPassword;
            RecoveryKey = string.Empty;
            NewPassword = string.Empty;
            NewPasswordConfirm = string.Empty;
            IsRecoveryMode = false;

            RecoveryKeyDisplay = newRecoveryKey;
            IsRecoveryKeyModalVisible = true;
        }
        catch (Exception)
        {
            ErrorMessage = _localizationService.Format("Lock.Recovery.Error.ResetFailed");
        }
        finally
        {
            IsUnlocking = false;
        }
    }

    [RelayCommand]
    private void CopyRecoveryKey()
    {
        CopyRecoveryKeyRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task DismissRecoveryKeyModalAsync()
    {
        IsRecoveryKeyModalVisible = false;
        RecoveryKeyDisplay = string.Empty;

        if (_pendingUnlockPassword != null)
        {
            await _lockService.UnlockAsync(_pendingUnlockPassword);
            _pendingUnlockPassword = null;
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
