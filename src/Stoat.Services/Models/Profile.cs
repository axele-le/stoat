namespace Stoat.Services.Models;

/// <summary>
/// Represents a user profile containing cryptographic credentials.
/// The Secret and Salt are stored encrypted using Data Protection API.
/// </summary>
public class Profile
{
    /// <summary>
    /// Unique identifier for the profile.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Human-readable name for the profile.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the profile's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The secret key for encryption operations (stored protected with DPAPI).
    /// </summary>
    public string ProtectedSecret { get; set; } = string.Empty;

    /// <summary>
    /// The salt for key derivation (stored protected with DPAPI).
    /// </summary>
    public string ProtectedSalt { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the profile was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp of the last modification.
    /// </summary>
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>
    /// UTC timestamp of the last time this profile was used.
    /// </summary>
    public DateTime? LastUsedAtUtc { get; set; }

    /// <summary>
    /// Version number for optimistic concurrency and migration support.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Per-profile encryption settings (cipher, KDF, parameters).
    /// Null means default settings should be used.
    /// </summary>
    public EncryptionSettings? EncryptionSettings { get; set; }

    /// <summary>
    /// Per-profile saved encryption presets.
    /// Null for backward compatibility with schema version 1.
    /// </summary>
    public List<EncryptionPreset>? EncryptionPresets { get; set; }

    /// <summary>
    /// ID of the currently selected encryption preset for this profile.
    /// Null means "Personalizzato" (custom settings).
    /// </summary>
    public Guid? SelectedPresetId { get; set; }
}
