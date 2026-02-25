namespace Stoat.Services.Models;

/// <summary>
/// Password-protected export format for profile migration.
/// The entire payload is encrypted with a user-provided password.
/// </summary>
public class ExportPackage
{
    /// <summary>
    /// Format version for future compatibility.
    /// </summary>
    public int FormatVersion { get; set; } = 1;

    /// <summary>
    /// UTC timestamp when the export was created.
    /// </summary>
    public DateTime ExportedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Name of the source machine (for user reference).
    /// </summary>
    public string? SourceMachineName { get; set; }

    /// <summary>
    /// Random salt used for password-based key derivation (Base64).
    /// </summary>
    public string PasswordSalt { get; set; } = string.Empty;

    /// <summary>
    /// PBKDF2 iteration count used for key derivation.
    /// </summary>
    public int KeyDerivationIterations { get; set; } = 100000;

    /// <summary>
    /// The encrypted payload containing the profiles (Base64).
    /// Decrypts to ExportPayload JSON.
    /// </summary>
    public string EncryptedPayload { get; set; } = string.Empty;

    /// <summary>
    /// HMAC-SHA256 of the encrypted payload for integrity verification (Base64).
    /// Empty when using GCM (provides built-in authentication).
    /// </summary>
    public string PayloadHmac { get; set; } = string.Empty;

    /// <summary>
    /// Encryption algorithm used for the payload.
    /// Legacy: "AES-CBC", Current: "AES-GCM".
    /// </summary>
    public string EncryptionAlgorithm { get; set; } = "AES-CBC";
}

/// <summary>
/// The decrypted content of an export package.
/// </summary>
public class ExportPayload
{
    /// <summary>
    /// List of profiles with their secrets in plaintext.
    /// </summary>
    public List<ExportedProfile> Profiles { get; set; } = new();

    /// <summary>
    /// The ID of the active profile at export time.
    /// </summary>
    public Guid? ActiveProfileId { get; set; }
}

/// <summary>
/// A profile in plaintext form for export.
/// </summary>
public class ExportedProfile
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Secret { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public EncryptionSettings? EncryptionSettings { get; set; }
    public List<EncryptionPreset>? EncryptionPresets { get; set; }
    public Guid? SelectedPresetId { get; set; }
}
