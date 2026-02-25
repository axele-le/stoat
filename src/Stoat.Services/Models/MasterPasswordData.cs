namespace Stoat.Services.Models;

/// <summary>
/// DPAPI-protected storage for master password hash and salt.
/// The actual password is never stored — only the PBKDF2 hash for verification.
/// </summary>
public class MasterPasswordData
{
    /// <summary>
    /// PBKDF2-SHA256 hash of the master password (Base64).
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Random salt used for PBKDF2 derivation (Base64).
    /// </summary>
    public string Salt { get; set; } = string.Empty;

    /// <summary>
    /// Number of PBKDF2 iterations used.
    /// </summary>
    public int Iterations { get; set; } = 200_000;

    /// <summary>
    /// PBKDF2-SHA256 hash of the recovery key (Base64).
    /// </summary>
    public string RecoveryKeyHash { get; set; } = string.Empty;

    /// <summary>
    /// Random salt used for recovery key PBKDF2 derivation (Base64).
    /// </summary>
    public string RecoveryKeySalt { get; set; } = string.Empty;
}
