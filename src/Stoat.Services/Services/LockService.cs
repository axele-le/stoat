using System.Security.Cryptography;
using System.Text;
using Stoat.Services.Interfaces;
using Stoat.Services.Models;

namespace Stoat.Services.Services;

/// <summary>
/// Manages application lock/unlock with master password verification (PBKDF2-SHA256)
/// and inactivity timer that clears credentials from memory.
/// </summary>
public class LockService : ILockService
{
    private readonly ISecureStorageService _storage;
    private readonly ProfileService _profileService;

    private const string MasterPasswordFileName = "master-password.dat";
    private const int Pbkdf2Iterations = 200_000;
    private const int HashLength = 32;
    private const int SaltLength = 32;
    private const int TimerIntervalSeconds = 10;
    private const int RecoveryKeyLength = 24; // 24 Base32 characters
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    private MasterPasswordData? _masterPasswordData;
    private bool _isLocked;
    private Timer? _inactivityTimer;
    private DateTime _lastActivityTime;
    private int _timeoutMinutes;

    public bool IsLocked => _isLocked;
    public bool HasMasterPassword => _masterPasswordData != null;

    public event EventHandler<bool>? LockStateChanged;

    public LockService(ISecureStorageService storage, ProfileService profileService)
    {
        _storage = storage;
        _profileService = profileService;
        _lastActivityTime = DateTime.UtcNow;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        _masterPasswordData = await _storage.LoadProtectedFileAsync<MasterPasswordData>(MasterPasswordFileName, ct);
    }

    public async Task SetupMasterPasswordAsync(string password, CancellationToken ct = default)
    {
        if (_masterPasswordData != null)
            throw new InvalidOperationException("Master password already configured. Use ChangeMasterPasswordAsync.");

        var saltBytes = new byte[SaltLength];
        RandomNumberGenerator.Fill(saltBytes);
        var salt = Convert.ToBase64String(saltBytes);

        var hash = ComputeHash(password, saltBytes, Pbkdf2Iterations);

        _masterPasswordData = new MasterPasswordData
        {
            PasswordHash = hash,
            Salt = salt,
            Iterations = Pbkdf2Iterations
        };

        await _storage.SaveProtectedFileAsync(MasterPasswordFileName, _masterPasswordData, ct);
    }

    public async Task<string> SetupMasterPasswordWithRecoveryAsync(string password, CancellationToken ct = default)
    {
        if (_masterPasswordData != null)
            throw new InvalidOperationException("Master password already configured. Use ChangeMasterPasswordWithRecoveryAsync.");

        var saltBytes = new byte[SaltLength];
        RandomNumberGenerator.Fill(saltBytes);
        var salt = Convert.ToBase64String(saltBytes);
        var hash = ComputeHash(password, saltBytes, Pbkdf2Iterations);

        var recoveryKey = GenerateRecoveryKey();
        var recoverySaltBytes = new byte[SaltLength];
        RandomNumberGenerator.Fill(recoverySaltBytes);
        var recoverySalt = Convert.ToBase64String(recoverySaltBytes);
        var recoveryHash = ComputeHash(NormalizeRecoveryKey(recoveryKey), recoverySaltBytes, Pbkdf2Iterations);

        _masterPasswordData = new MasterPasswordData
        {
            PasswordHash = hash,
            Salt = salt,
            Iterations = Pbkdf2Iterations,
            RecoveryKeyHash = recoveryHash,
            RecoveryKeySalt = recoverySalt
        };

        await _storage.SaveProtectedFileAsync(MasterPasswordFileName, _masterPasswordData, ct);
        return recoveryKey;
    }

    public async Task<bool> ChangeMasterPasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default)
    {
        if (_masterPasswordData == null) return false;
        if (!VerifyPassword(currentPassword)) return false;

        var saltBytes = new byte[SaltLength];
        RandomNumberGenerator.Fill(saltBytes);
        var salt = Convert.ToBase64String(saltBytes);
        var hash = ComputeHash(newPassword, saltBytes, Pbkdf2Iterations);

        _masterPasswordData = new MasterPasswordData
        {
            PasswordHash = hash,
            Salt = salt,
            Iterations = Pbkdf2Iterations,
            RecoveryKeyHash = _masterPasswordData.RecoveryKeyHash,
            RecoveryKeySalt = _masterPasswordData.RecoveryKeySalt
        };

        await _storage.SaveProtectedFileAsync(MasterPasswordFileName, _masterPasswordData, ct);
        return true;
    }

    public async Task<string?> ChangeMasterPasswordWithRecoveryAsync(string currentPassword, string newPassword, CancellationToken ct = default)
    {
        if (_masterPasswordData == null) return null;
        if (!VerifyPassword(currentPassword)) return null;

        var saltBytes = new byte[SaltLength];
        RandomNumberGenerator.Fill(saltBytes);
        var salt = Convert.ToBase64String(saltBytes);
        var hash = ComputeHash(newPassword, saltBytes, Pbkdf2Iterations);

        var recoveryKey = GenerateRecoveryKey();
        var recoverySaltBytes = new byte[SaltLength];
        RandomNumberGenerator.Fill(recoverySaltBytes);
        var recoverySalt = Convert.ToBase64String(recoverySaltBytes);
        var recoveryHash = ComputeHash(NormalizeRecoveryKey(recoveryKey), recoverySaltBytes, Pbkdf2Iterations);

        _masterPasswordData = new MasterPasswordData
        {
            PasswordHash = hash,
            Salt = salt,
            Iterations = Pbkdf2Iterations,
            RecoveryKeyHash = recoveryHash,
            RecoveryKeySalt = recoverySalt
        };

        await _storage.SaveProtectedFileAsync(MasterPasswordFileName, _masterPasswordData, ct);
        return recoveryKey;
    }

    public Task<bool> VerifyRecoveryKeyAsync(string recoveryKey, CancellationToken ct = default)
    {
        return Task.FromResult(VerifyRecoveryKey(recoveryKey));
    }

    public async Task<string?> ResetPasswordWithRecoveryKeyAsync(string recoveryKey, string newPassword, CancellationToken ct = default)
    {
        if (_masterPasswordData == null) return null;
        if (!VerifyRecoveryKey(recoveryKey)) return null;

        var saltBytes = new byte[SaltLength];
        RandomNumberGenerator.Fill(saltBytes);
        var salt = Convert.ToBase64String(saltBytes);
        var hash = ComputeHash(newPassword, saltBytes, Pbkdf2Iterations);

        var newRecoveryKey = GenerateRecoveryKey();
        var recoverySaltBytes = new byte[SaltLength];
        RandomNumberGenerator.Fill(recoverySaltBytes);
        var recoverySalt = Convert.ToBase64String(recoverySaltBytes);
        var recoveryHash = ComputeHash(NormalizeRecoveryKey(newRecoveryKey), recoverySaltBytes, Pbkdf2Iterations);

        _masterPasswordData = new MasterPasswordData
        {
            PasswordHash = hash,
            Salt = salt,
            Iterations = Pbkdf2Iterations,
            RecoveryKeyHash = recoveryHash,
            RecoveryKeySalt = recoverySalt
        };

        await _storage.SaveProtectedFileAsync(MasterPasswordFileName, _masterPasswordData, ct);
        return newRecoveryKey;
    }

    public async Task<bool> RemoveMasterPasswordAsync(string currentPassword, CancellationToken ct = default)
    {
        if (_masterPasswordData == null) return false;
        if (!VerifyPassword(currentPassword)) return false;

        _masterPasswordData = null;
        await _storage.DeleteProtectedFileAsync(MasterPasswordFileName, ct);

        if (_isLocked)
        {
            _isLocked = false;
            await _profileService.RestoreActiveCredentialsAsync(ct);
            LockStateChanged?.Invoke(this, false);
        }

        StopActivityTimer();
        return true;
    }

    public Task<bool> VerifyPasswordAsync(string password, CancellationToken ct = default)
    {
        return Task.FromResult(VerifyPassword(password));
    }

    public async Task LockAsync()
    {
        if (_isLocked) return;

        _profileService.ClearActiveCredentials();
        _isLocked = true;
        LockStateChanged?.Invoke(this, true);
    }

    public async Task<bool> UnlockAsync(string password, CancellationToken ct = default)
    {
        if (!_isLocked) return true;
        if (_masterPasswordData == null) return false;
        if (!VerifyPassword(password)) return false;

        await _profileService.RestoreActiveCredentialsAsync(ct);
        _isLocked = false;
        ResetActivityTimer();
        LockStateChanged?.Invoke(this, false);
        return true;
    }

    public void ResetActivityTimer()
    {
        _lastActivityTime = DateTime.UtcNow;
    }

    public void StartActivityTimer(int timeoutMinutes)
    {
        _timeoutMinutes = timeoutMinutes;
        StopActivityTimer();
        _lastActivityTime = DateTime.UtcNow;
        _inactivityTimer = new Timer(CheckInactivity, null,
            TimeSpan.FromSeconds(TimerIntervalSeconds),
            TimeSpan.FromSeconds(TimerIntervalSeconds));
    }

    public void StopActivityTimer()
    {
        _inactivityTimer?.Dispose();
        _inactivityTimer = null;
    }

    private void CheckInactivity(object? state)
    {
        if (_isLocked || _masterPasswordData == null) return;

        var elapsed = DateTime.UtcNow - _lastActivityTime;
        if (elapsed.TotalMinutes >= _timeoutMinutes)
        {
            _ = LockAsync();
        }
    }

    private bool VerifyPassword(string password)
    {
        if (_masterPasswordData == null) return false;

        var saltBytes = Convert.FromBase64String(_masterPasswordData.Salt);
        var computedHash = ComputeHash(password, saltBytes, _masterPasswordData.Iterations);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHash),
            Encoding.UTF8.GetBytes(_masterPasswordData.PasswordHash));
    }

    private bool VerifyRecoveryKey(string recoveryKey)
    {
        if (_masterPasswordData == null) return false;
        if (string.IsNullOrEmpty(_masterPasswordData.RecoveryKeyHash) ||
            string.IsNullOrEmpty(_masterPasswordData.RecoveryKeySalt))
            return false;

        var saltBytes = Convert.FromBase64String(_masterPasswordData.RecoveryKeySalt);
        var computedHash = ComputeHash(NormalizeRecoveryKey(recoveryKey), saltBytes, _masterPasswordData.Iterations);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHash),
            Encoding.UTF8.GetBytes(_masterPasswordData.RecoveryKeyHash));
    }

    private static string GenerateRecoveryKey()
    {
        var bytes = new byte[RecoveryKeyLength];
        RandomNumberGenerator.Fill(bytes);

        var chars = new char[RecoveryKeyLength];
        for (int i = 0; i < RecoveryKeyLength; i++)
        {
            chars[i] = Base32Alphabet[bytes[i] % Base32Alphabet.Length];
        }

        // Format as XXXX-XXXX-XXXX-XXXX-XXXX-XXXX
        var raw = new string(chars);
        return string.Join("-", Enumerable.Range(0, 6).Select(i => raw.Substring(i * 4, 4)));
    }

    private static string NormalizeRecoveryKey(string recoveryKey)
    {
        return recoveryKey.Replace("-", "").Trim().ToUpperInvariant();
    }

    private static string ComputeHash(string password, byte[] salt, int iterations)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(HashLength);
        return Convert.ToBase64String(hash);
    }
}
