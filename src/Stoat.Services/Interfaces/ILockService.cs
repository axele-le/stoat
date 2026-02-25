namespace Stoat.Services.Interfaces;

/// <summary>
/// Service for application lock/unlock with master password and inactivity timer.
/// </summary>
public interface ILockService
{
    /// <summary>
    /// Whether the application is currently locked.
    /// </summary>
    bool IsLocked { get; }

    /// <summary>
    /// Whether a master password has been configured.
    /// </summary>
    bool HasMasterPassword { get; }

    /// <summary>
    /// Fired when lock state changes (locked/unlocked).
    /// </summary>
    event EventHandler<bool> LockStateChanged;

    /// <summary>
    /// Sets up a new master password. Fails if one already exists (use ChangeMasterPasswordAsync).
    /// </summary>
    Task SetupMasterPasswordAsync(string password, CancellationToken ct = default);

    /// <summary>
    /// Sets up a new master password and generates a recovery key.
    /// Returns the recovery key in plain text (formatted with dashes).
    /// </summary>
    Task<string> SetupMasterPasswordWithRecoveryAsync(string password, CancellationToken ct = default);

    /// <summary>
    /// Changes the master password. Requires verification of the current password.
    /// </summary>
    Task<bool> ChangeMasterPasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default);

    /// <summary>
    /// Changes the master password and generates a new recovery key.
    /// Returns the new recovery key in plain text, or null if current password is wrong.
    /// </summary>
    Task<string?> ChangeMasterPasswordWithRecoveryAsync(string currentPassword, string newPassword, CancellationToken ct = default);

    /// <summary>
    /// Verifies a recovery key against the stored hash.
    /// </summary>
    Task<bool> VerifyRecoveryKeyAsync(string recoveryKey, CancellationToken ct = default);

    /// <summary>
    /// Resets the master password using a valid recovery key.
    /// Returns the new recovery key in plain text, or null if the recovery key is invalid.
    /// </summary>
    Task<string?> ResetPasswordWithRecoveryKeyAsync(string recoveryKey, string newPassword, CancellationToken ct = default);

    /// <summary>
    /// Removes the master password. Requires verification of the current password.
    /// Also disables auto-lock.
    /// </summary>
    Task<bool> RemoveMasterPasswordAsync(string currentPassword, CancellationToken ct = default);

    /// <summary>
    /// Verifies a password against the stored master password hash.
    /// </summary>
    Task<bool> VerifyPasswordAsync(string password, CancellationToken ct = default);

    /// <summary>
    /// Locks the application: clears active credentials from memory.
    /// </summary>
    Task LockAsync();

    /// <summary>
    /// Unlocks the application with the master password: verifies and restores credentials.
    /// </summary>
    Task<bool> UnlockAsync(string password, CancellationToken ct = default);

    /// <summary>
    /// Resets the inactivity timer. Call on every user interaction.
    /// </summary>
    void ResetActivityTimer();

    /// <summary>
    /// Starts the inactivity timer with the specified timeout.
    /// </summary>
    void StartActivityTimer(int timeoutMinutes);

    /// <summary>
    /// Stops the inactivity timer.
    /// </summary>
    void StopActivityTimer();

    /// <summary>
    /// Initializes the service: loads master password data from storage.
    /// </summary>
    Task InitializeAsync(CancellationToken ct = default);
}
