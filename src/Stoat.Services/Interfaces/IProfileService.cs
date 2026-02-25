using Stoat.Services.Models;

namespace Stoat.Services.Interfaces;

/// <summary>
/// Service for managing user profiles including CRUD operations,
/// active profile switching, and export/import functionality.
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Event raised when the active profile changes.
    /// </summary>
    event EventHandler<ProfileChangedEventArgs>? ActiveProfileChanged;

    /// <summary>
    /// Gets the currently active profile, or null if none is set.
    /// </summary>
    Profile? ActiveProfile { get; }

    /// <summary>
    /// Gets all profiles.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Read-only list of all profiles.</returns>
    Task<IReadOnlyList<Profile>> GetAllProfilesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a profile by its ID.
    /// </summary>
    /// <param name="id">The profile ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The profile, or null if not found.</returns>
    Task<Profile?> GetProfileByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets the decrypted credentials (secret and salt) for a profile.
    /// </summary>
    /// <param name="id">The profile ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The decrypted secret and salt, or null if profile not found.</returns>
    Task<(string secret, string salt)?> GetProfileCredentialsAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Updates the credentials (secret and/or salt) for a profile.
    /// Warning: Data encrypted with old credentials will no longer be decryptable.
    /// </summary>
    /// <param name="id">The profile ID.</param>
    /// <param name="newSecret">The new secret (null to keep existing).</param>
    /// <param name="newSalt">The new salt (null to keep existing).</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateProfileCredentialsAsync(Guid id, string? newSecret, string? newSalt, CancellationToken ct = default);

    /// <summary>
    /// Creates a new profile with auto-generated secret and salt.
    /// </summary>
    /// <param name="name">Profile name.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="setAsActive">If true, sets this as the active profile.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created profile.</returns>
    Task<Profile> CreateProfileAsync(string name, string? description = null,
        bool setAsActive = false, CancellationToken ct = default);

    /// <summary>
    /// Creates a new profile with custom secret and salt.
    /// </summary>
    /// <param name="name">Profile name.</param>
    /// <param name="secret">The encryption secret.</param>
    /// <param name="salt">The encryption salt.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="setAsActive">If true, sets this as the active profile.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created profile.</returns>
    Task<Profile> CreateProfileWithCredentialsAsync(string name, string secret,
        string salt, string? description = null, bool setAsActive = false,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing profile's metadata (name, description).
    /// </summary>
    /// <param name="id">The profile ID.</param>
    /// <param name="name">New name.</param>
    /// <param name="description">New description.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated profile.</returns>
    Task<Profile> UpdateProfileAsync(Guid id, string name, string? description,
        CancellationToken ct = default);

    /// <summary>
    /// Rotates the secret and salt for an existing profile.
    /// Warning: Data encrypted with old credentials will no longer be decryptable.
    /// </summary>
    /// <param name="id">The profile ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The new credentials (secret, salt) for user reference.</returns>
    Task<(string newSecret, string newSalt)> RotateCredentialsAsync(Guid id,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a profile.
    /// </summary>
    /// <param name="id">The profile ID to delete.</param>
    /// <param name="newActiveProfileId">If deleting the active profile, specify which profile becomes active.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteProfileAsync(Guid id, Guid? newActiveProfileId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sets the active profile by ID.
    /// </summary>
    /// <param name="id">The profile ID to activate.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetActiveProfileAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Exports selected profiles to a password-protected package.
    /// </summary>
    /// <param name="profileIds">IDs of profiles to export.</param>
    /// <param name="password">Password for encryption.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The export package.</returns>
    Task<ExportPackage> ExportProfilesAsync(IEnumerable<Guid> profileIds,
        string password, CancellationToken ct = default);

    /// <summary>
    /// Exports all profiles to a password-protected package.
    /// </summary>
    /// <param name="password">Password for encryption.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The export package.</returns>
    Task<ExportPackage> ExportAllProfilesAsync(string password,
        CancellationToken ct = default);

    /// <summary>
    /// Imports profiles from a password-protected package.
    /// </summary>
    /// <param name="package">The export package to import.</param>
    /// <param name="password">The password to decrypt the package.</param>
    /// <param name="conflictResolution">How to handle name conflicts.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Import result with counts and warnings.</returns>
    Task<ImportResult> ImportProfilesAsync(ExportPackage package, string password,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Rename,
        CancellationToken ct = default);

    /// <summary>
    /// Saves encryption settings for a specific profile.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <param name="settings">The encryption settings to save.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SaveEncryptionSettingsAsync(Guid profileId, EncryptionSettings settings, CancellationToken ct = default);

    /// <summary>
    /// Gets the encryption settings for a specific profile.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The encryption settings, or null if not configured (use defaults).</returns>
    Task<EncryptionSettings?> GetEncryptionSettingsAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>
    /// Gets the encryption presets saved for a specific profile.
    /// </summary>
    Task<IReadOnlyList<EncryptionPreset>> GetPresetsAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>
    /// Saves a new encryption preset for a specific profile.
    /// </summary>
    Task<EncryptionPreset> SavePresetAsync(Guid profileId, string name, EncryptionSettings settings, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing encryption preset's settings.
    /// </summary>
    Task UpdatePresetAsync(Guid profileId, Guid presetId, EncryptionSettings settings, CancellationToken ct = default);

    /// <summary>
    /// Deletes an encryption preset from a profile.
    /// </summary>
    Task DeletePresetAsync(Guid profileId, Guid presetId, CancellationToken ct = default);

    /// <summary>
    /// Gets the currently selected preset ID for a profile.
    /// Returns null if "Personalizzato" (custom) is selected.
    /// </summary>
    Task<Guid?> GetSelectedPresetIdAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>
    /// Sets the currently selected preset ID for a profile.
    /// Pass null to select "Personalizzato" (custom).
    /// </summary>
    Task SetSelectedPresetIdAsync(Guid profileId, Guid? presetId, CancellationToken ct = default);

    /// <summary>
    /// Initializes the profile service, loading existing profiles.
    /// Should be called during application startup.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task InitializeAsync(CancellationToken ct = default);
}

/// <summary>
/// Event args for profile change events.
/// </summary>
public class ProfileChangedEventArgs : EventArgs
{
    public Profile? OldProfile { get; init; }
    public Profile? NewProfile { get; init; }
}

/// <summary>
/// Conflict resolution strategy for import operations.
/// </summary>
public enum ImportConflictResolution
{
    /// <summary>Rename imported profiles that conflict with existing ones.</summary>
    Rename,
    /// <summary>Replace existing profiles with imported ones.</summary>
    Replace,
    /// <summary>Skip profiles that conflict with existing ones.</summary>
    Skip
}

/// <summary>
/// Result of an import operation.
/// </summary>
public class ImportResult
{
    public int ImportedCount { get; init; }
    public int SkippedCount { get; init; }
    public int ReplacedCount { get; init; }
    public List<string> Warnings { get; init; } = new();
}
