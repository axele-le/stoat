using System.Security.Cryptography;
using System.Text.Json;
using Stoat.Services.Exceptions;
using Stoat.Services.Interfaces;
using Stoat.Services.Models;

namespace Stoat.Services.Services;

/// <summary>
/// Service for managing user profiles including CRUD operations,
/// active profile switching, and export/import functionality.
/// </summary>
public class ProfileService : IProfileService
{
    private readonly ISecureStorageService _storage;
    private readonly IApiKeyService _apiKeyService;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private const string ProfilesFileName = "profiles.dat";
    private const string LegacyPresetsFileName = "encryption-presets.dat";

    private ProfileCollection? _profileCollection;
    private Profile? _activeProfile;
    private (string secret, string salt)? _activeCredentials;

    /// <inheritdoc />
    public event EventHandler<ProfileChangedEventArgs>? ActiveProfileChanged;

    /// <inheritdoc />
    public Profile? ActiveProfile => _activeProfile;

    public ProfileService(ISecureStorageService storage, IApiKeyService apiKeyService)
    {
        _storage = storage;
        _apiKeyService = apiKeyService;
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _profileCollection = await _storage.LoadProtectedFileAsync<ProfileCollection>(ProfilesFileName, ct);

            if (_profileCollection == null)
            {
                // First run - create default profile
                _profileCollection = new ProfileCollection();
                await CreateProfileInternalAsync("Default", "Default profile", setAsActive: true, ct);
            }
            else
            {
                // Migrate from schema v1 to v2: move global presets into each profile
                if (_profileCollection.SchemaVersion < 2)
                {
                    await MigratePresetsToProfilesAsync(ct);
                }

                if (_profileCollection.ActiveProfileId.HasValue)
                {
                    // Load active profile
                    var activeProfile = _profileCollection.Profiles
                        .FirstOrDefault(p => p.Id == _profileCollection.ActiveProfileId);

                    if (activeProfile != null)
                    {
                        await SetActiveProfileInternalAsync(activeProfile, ct);
                    }
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Profile>> GetAllProfilesAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            return _profileCollection!.Profiles.AsReadOnly();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Profile?> GetProfileByIdAsync(Guid id, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            return _profileCollection!.Profiles.FirstOrDefault(p => p.Id == id);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<(string secret, string salt)?> GetProfileCredentialsAsync(Guid id, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profile = _profileCollection!.Profiles.FirstOrDefault(p => p.Id == id);
            if (profile == null) return null;

            var secret = _storage.Unprotect(profile.ProtectedSecret);
            var salt = _storage.Unprotect(profile.ProtectedSalt);
            return (secret, salt);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpdateProfileCredentialsAsync(Guid id, string? newSecret, string? newSalt, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();

            var profile = _profileCollection!.Profiles.FirstOrDefault(p => p.Id == id)
                ?? throw new ProfileNotFoundException(id);

            // Trim per rimuovere spazi bianchi accidentali
            newSecret = newSecret?.Trim();
            newSalt = newSalt?.Trim();

            if (!string.IsNullOrEmpty(newSecret))
            {
                profile.ProtectedSecret = _storage.Protect(newSecret);
            }

            if (!string.IsNullOrEmpty(newSalt))
            {
                profile.ProtectedSalt = _storage.Protect(newSalt);
            }

            if (!string.IsNullOrEmpty(newSecret) || !string.IsNullOrEmpty(newSalt))
            {
                profile.ModifiedAtUtc = DateTime.UtcNow;
                profile.Version++;
                _profileCollection.LastModifiedUtc = DateTime.UtcNow;
                await SaveProfileCollectionAsync(ct);

                // If this is the active profile, update cached credentials
                if (_activeProfile?.Id == id)
                {
                    var secret = !string.IsNullOrEmpty(newSecret) ? newSecret : _storage.Unprotect(profile.ProtectedSecret);
                    var salt = !string.IsNullOrEmpty(newSalt) ? newSalt : _storage.Unprotect(profile.ProtectedSalt);
                    _activeCredentials = (secret, salt);
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Profile> CreateProfileAsync(string name, string? description = null,
        bool setAsActive = false, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            return await CreateProfileInternalAsync(name, description, setAsActive, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Profile> CreateProfileWithCredentialsAsync(string name, string secret,
        string salt, string? description = null, bool setAsActive = false,
        CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();

            var profile = new Profile
            {
                Name = name,
                Description = description,
                ProtectedSecret = _storage.Protect(secret),
                ProtectedSalt = _storage.Protect(salt),
                CreatedAtUtc = DateTime.UtcNow
            };

            _profileCollection!.Profiles.Add(profile);
            _profileCollection.LastModifiedUtc = DateTime.UtcNow;

            await SaveProfileCollectionAsync(ct);

            if (setAsActive)
            {
                await SetActiveProfileInternalAsync(profile, ct);
            }

            return profile;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Profile> UpdateProfileAsync(Guid id, string name, string? description,
        CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();

            var profile = _profileCollection!.Profiles.FirstOrDefault(p => p.Id == id)
                ?? throw new ProfileNotFoundException(id);

            profile.Name = name;
            profile.Description = description;
            profile.ModifiedAtUtc = DateTime.UtcNow;

            _profileCollection.LastModifiedUtc = DateTime.UtcNow;
            await SaveProfileCollectionAsync(ct);

            return profile;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<(string newSecret, string newSalt)> RotateCredentialsAsync(Guid id,
        CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();

            var profile = _profileCollection!.Profiles.FirstOrDefault(p => p.Id == id)
                ?? throw new ProfileNotFoundException(id);

            // Generate new credentials
            var (newSecret, newSalt) = _apiKeyService.GenerateCredentialPair();

            // Update profile
            profile.ProtectedSecret = _storage.Protect(newSecret);
            profile.ProtectedSalt = _storage.Protect(newSalt);
            profile.ModifiedAtUtc = DateTime.UtcNow;
            profile.Version++;

            _profileCollection.LastModifiedUtc = DateTime.UtcNow;
            await SaveProfileCollectionAsync(ct);

            // If this is the active profile, update cached credentials
            if (_activeProfile?.Id == id)
            {
                _activeCredentials = (newSecret, newSalt);
            }

            return (newSecret, newSalt);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task DeleteProfileAsync(Guid id, Guid? newActiveProfileId = null,
        CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();

            var profile = _profileCollection!.Profiles.FirstOrDefault(p => p.Id == id)
                ?? throw new ProfileNotFoundException(id);

            // Check if this is the active profile
            if (_activeProfile?.Id == id)
            {
                if (newActiveProfileId.HasValue)
                {
                    var newActive = _profileCollection.Profiles.FirstOrDefault(p => p.Id == newActiveProfileId)
                        ?? throw new ProfileNotFoundException(newActiveProfileId.Value);

                    await SetActiveProfileInternalAsync(newActive, ct);
                }
                else if (_profileCollection.Profiles.Count > 1)
                {
                    // Set another profile as active
                    var newActive = _profileCollection.Profiles.First(p => p.Id != id);
                    await SetActiveProfileInternalAsync(newActive, ct);
                }
                else
                {
                    throw new ProfileException("Cannot delete the only profile. Create another profile first.");
                }
            }

            _profileCollection.Profiles.Remove(profile);
            _profileCollection.LastModifiedUtc = DateTime.UtcNow;

            await SaveProfileCollectionAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task SetActiveProfileAsync(Guid id, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();

            var profile = _profileCollection!.Profiles.FirstOrDefault(p => p.Id == id)
                ?? throw new ProfileNotFoundException(id);

            await SetActiveProfileInternalAsync(profile, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ExportPackage> ExportProfilesAsync(IEnumerable<Guid> profileIds,
        string password, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();

            var idsToExport = profileIds.ToHashSet();
            var profilesToExport = _profileCollection!.Profiles
                .Where(p => idsToExport.Contains(p.Id))
                .ToList();

            if (profilesToExport.Count == 0)
                throw new ExportImportException("No valid profiles found to export.");

            return CreateExportPackage(profilesToExport, password);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ExportPackage> ExportAllProfilesAsync(string password,
        CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();

            if (_profileCollection!.Profiles.Count == 0)
                throw new ExportImportException("No profiles to export.");

            return CreateExportPackage(_profileCollection.Profiles, password);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ImportResult> ImportProfilesAsync(ExportPackage package, string password,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Rename,
        CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();

            // Decrypt payload based on encryption algorithm
            string payloadJson;
            try
            {
                if (package.FormatVersion >= 2 && package.EncryptionAlgorithm == "AES-GCM")
                {
                    // AES-256-GCM (authentication is built-in)
                    payloadJson = _storage.DecryptWithPasswordGcm(
                        package.EncryptedPayload,
                        password,
                        package.PasswordSalt,
                        package.KeyDerivationIterations);
                }
                else
                {
                    // Legacy: AES-CBC + HMAC
                    if (!_storage.VerifyHmac(package.EncryptedPayload, password, package.PayloadHmac))
                        throw new InvalidExportPasswordException();

                    payloadJson = _storage.DecryptWithPassword(
                        package.EncryptedPayload,
                        password,
                        package.PasswordSalt,
                        package.KeyDerivationIterations);
                }
            }
            catch (InvalidExportPasswordException)
            {
                throw;
            }
            catch (CryptographicException)
            {
                throw new InvalidExportPasswordException();
            }

            var payload = JsonSerializer.Deserialize<ExportPayload>(payloadJson,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
                ?? throw new ExportImportException("Failed to deserialize export payload.");

            var result = new ImportResult { Warnings = new List<string>() };
            var importedCount = 0;
            var skippedCount = 0;
            var replacedCount = 0;
            var warnings = new List<string>();

            foreach (var exportedProfile in payload.Profiles)
            {
                var existingByName = _profileCollection!.Profiles
                    .FirstOrDefault(p => p.Name.Equals(exportedProfile.Name, StringComparison.OrdinalIgnoreCase));

                if (existingByName != null)
                {
                    switch (conflictResolution)
                    {
                        case ImportConflictResolution.Skip:
                            skippedCount++;
                            warnings.Add($"Skipped '{exportedProfile.Name}' - already exists.");
                            continue;

                        case ImportConflictResolution.Replace:
                            _profileCollection.Profiles.Remove(existingByName);
                            replacedCount++;
                            break;

                        case ImportConflictResolution.Rename:
                            exportedProfile.Name = GenerateUniqueName(exportedProfile.Name);
                            break;
                    }
                }

                // Import presets with new Guids to avoid collisions
                List<EncryptionPreset>? importedPresets = null;
                if (exportedProfile.EncryptionPresets is { Count: > 0 })
                {
                    importedPresets = exportedProfile.EncryptionPresets.Select(p => new EncryptionPreset
                    {
                        Id = Guid.NewGuid(),
                        Name = p.Name,
                        Settings = p.Settings.Clone(),
                        CreatedAtUtc = p.CreatedAtUtc
                    }).ToList();
                }

                // Create new profile with protected credentials
                var newProfile = new Profile
                {
                    Id = Guid.NewGuid(),
                    Name = exportedProfile.Name,
                    Description = exportedProfile.Description,
                    ProtectedSecret = _storage.Protect(exportedProfile.Secret),
                    ProtectedSalt = _storage.Protect(exportedProfile.Salt),
                    CreatedAtUtc = exportedProfile.CreatedAtUtc,
                    ModifiedAtUtc = DateTime.UtcNow,
                    EncryptionSettings = exportedProfile.EncryptionSettings,
                    EncryptionPresets = importedPresets,
                    SelectedPresetId = null // Reset selection on import
                };

                _profileCollection.Profiles.Add(newProfile);
                importedCount++;
            }

            _profileCollection!.LastModifiedUtc = DateTime.UtcNow;
            await SaveProfileCollectionAsync(ct);

            return new ImportResult
            {
                ImportedCount = importedCount,
                SkippedCount = skippedCount,
                ReplacedCount = replacedCount,
                Warnings = warnings
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveEncryptionSettingsAsync(Guid profileId, EncryptionSettings settings, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();

            var profile = _profileCollection!.Profiles.FirstOrDefault(p => p.Id == profileId)
                ?? throw new ProfileNotFoundException(profileId);

            profile.EncryptionSettings = settings;
            profile.ModifiedAtUtc = DateTime.UtcNow;

            _profileCollection.LastModifiedUtc = DateTime.UtcNow;
            await SaveProfileCollectionAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<EncryptionSettings?> GetEncryptionSettingsAsync(Guid profileId, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();

            var profile = _profileCollection!.Profiles.FirstOrDefault(p => p.Id == profileId);
            return profile?.EncryptionSettings;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Gets the active profile's decrypted credentials.
    /// Used internally by EncryptionService.
    /// </summary>
    internal (string secret, string salt)? GetActiveCredentials() => _activeCredentials;

    /// <summary>
    /// Clears cached active credentials from memory.
    /// Used by LockService when the app is locked.
    /// </summary>
    internal void ClearActiveCredentials()
    {
        _activeCredentials = null;
    }

    /// <summary>
    /// Restores cached active credentials by re-decrypting from DPAPI storage.
    /// Used by LockService when the app is unlocked.
    /// </summary>
    internal async Task RestoreActiveCredentialsAsync(CancellationToken ct = default)
    {
        if (_activeProfile == null) return;

        var secret = _storage.Unprotect(_activeProfile.ProtectedSecret);
        var salt = _storage.Unprotect(_activeProfile.ProtectedSalt);
        _activeCredentials = (secret, salt);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EncryptionPreset>> GetPresetsAsync(Guid profileId, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profile = _profileCollection!.Profiles.FirstOrDefault(p => p.Id == profileId)
                ?? throw new ProfileNotFoundException(profileId);

            return (profile.EncryptionPresets ?? new List<EncryptionPreset>())
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<EncryptionPreset> SavePresetAsync(Guid profileId, string name, EncryptionSettings settings, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Preset name cannot be empty.", nameof(name));

        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profile = _profileCollection!.Profiles.FirstOrDefault(p => p.Id == profileId)
                ?? throw new ProfileNotFoundException(profileId);

            profile.EncryptionPresets ??= new List<EncryptionPreset>();

            var preset = new EncryptionPreset
            {
                Name = name.Trim(),
                Settings = settings.Clone(),
                CreatedAtUtc = DateTime.UtcNow
            };

            profile.EncryptionPresets.Add(preset);
            profile.ModifiedAtUtc = DateTime.UtcNow;
            _profileCollection.LastModifiedUtc = DateTime.UtcNow;
            await SaveProfileCollectionAsync(ct);

            return preset;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpdatePresetAsync(Guid profileId, Guid presetId, EncryptionSettings settings, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profile = _profileCollection!.Profiles.FirstOrDefault(p => p.Id == profileId)
                ?? throw new ProfileNotFoundException(profileId);

            var preset = profile.EncryptionPresets?.FirstOrDefault(p => p.Id == presetId)
                ?? throw new KeyNotFoundException($"Preset with ID {presetId} not found.");

            preset.Settings = settings.Clone();
            profile.ModifiedAtUtc = DateTime.UtcNow;
            _profileCollection.LastModifiedUtc = DateTime.UtcNow;
            await SaveProfileCollectionAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task DeletePresetAsync(Guid profileId, Guid presetId, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profile = _profileCollection!.Profiles.FirstOrDefault(p => p.Id == profileId)
                ?? throw new ProfileNotFoundException(profileId);

            var preset = profile.EncryptionPresets?.FirstOrDefault(p => p.Id == presetId)
                ?? throw new KeyNotFoundException($"Preset with ID {presetId} not found.");

            profile.EncryptionPresets!.Remove(preset);

            // Clear selection if the deleted preset was selected
            if (profile.SelectedPresetId == presetId)
                profile.SelectedPresetId = null;

            profile.ModifiedAtUtc = DateTime.UtcNow;
            _profileCollection.LastModifiedUtc = DateTime.UtcNow;
            await SaveProfileCollectionAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Guid?> GetSelectedPresetIdAsync(Guid profileId, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profile = _profileCollection!.Profiles.FirstOrDefault(p => p.Id == profileId);
            return profile?.SelectedPresetId;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task SetSelectedPresetIdAsync(Guid profileId, Guid? presetId, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profile = _profileCollection!.Profiles.FirstOrDefault(p => p.Id == profileId)
                ?? throw new ProfileNotFoundException(profileId);

            profile.SelectedPresetId = presetId;
            _profileCollection.LastModifiedUtc = DateTime.UtcNow;
            await SaveProfileCollectionAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<Profile> CreateProfileInternalAsync(string name, string? description,
        bool setAsActive, CancellationToken ct)
    {
        EnsureInitialized();

        var (secret, salt) = _apiKeyService.GenerateCredentialPair();

        var profile = new Profile
        {
            Name = name,
            Description = description,
            ProtectedSecret = _storage.Protect(secret),
            ProtectedSalt = _storage.Protect(salt),
            CreatedAtUtc = DateTime.UtcNow
        };

        _profileCollection!.Profiles.Add(profile);
        _profileCollection.LastModifiedUtc = DateTime.UtcNow;

        await SaveProfileCollectionAsync(ct);

        if (setAsActive)
        {
            await SetActiveProfileInternalAsync(profile, ct);
        }

        return profile;
    }

    private async Task SetActiveProfileInternalAsync(Profile profile, CancellationToken ct)
    {
        var oldProfile = _activeProfile;

        // Decrypt and cache credentials
        var secret = _storage.Unprotect(profile.ProtectedSecret);
        var salt = _storage.Unprotect(profile.ProtectedSalt);

        _activeProfile = profile;
        _activeCredentials = (secret, salt);
        _profileCollection!.ActiveProfileId = profile.Id;

        // Update last used time
        profile.LastUsedAtUtc = DateTime.UtcNow;

        await SaveProfileCollectionAsync(ct);

        ActiveProfileChanged?.Invoke(this, new ProfileChangedEventArgs
        {
            OldProfile = oldProfile,
            NewProfile = profile
        });
    }

    private ExportPackage CreateExportPackage(IEnumerable<Profile> profiles, string password)
    {
        var payload = new ExportPayload
        {
            ActiveProfileId = _profileCollection!.ActiveProfileId,
            Profiles = profiles.Select(p => new ExportedProfile
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Secret = _storage.Unprotect(p.ProtectedSecret),
                Salt = _storage.Unprotect(p.ProtectedSalt),
                CreatedAtUtc = p.CreatedAtUtc,
                EncryptionSettings = p.EncryptionSettings,
                EncryptionPresets = p.EncryptionPresets,
                SelectedPresetId = p.SelectedPresetId
            }).ToList()
        };

        var payloadJson = JsonSerializer.Serialize(payload,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var passwordSalt = _storage.GenerateSalt();
        var encryptedPayload = _storage.EncryptWithPasswordGcm(payloadJson, password, passwordSalt);

        return new ExportPackage
        {
            FormatVersion = 2,
            ExportedAtUtc = DateTime.UtcNow,
            SourceMachineName = Environment.MachineName,
            PasswordSalt = passwordSalt,
            KeyDerivationIterations = 200000,
            EncryptedPayload = encryptedPayload,
            PayloadHmac = string.Empty,
            EncryptionAlgorithm = "AES-GCM"
        };
    }

    private string GenerateUniqueName(string baseName)
    {
        var name = baseName;
        var counter = 1;

        while (_profileCollection!.Profiles.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            name = $"{baseName} ({counter++})";
        }

        return name;
    }

    private async Task SaveProfileCollectionAsync(CancellationToken ct)
    {
        await _storage.SaveProtectedFileAsync(ProfilesFileName, _profileCollection!, ct);
    }

    private async Task MigratePresetsToProfilesAsync(CancellationToken ct)
    {
        // Try to load legacy global presets file
        var legacyCollection = await _storage.LoadProtectedFileAsync<LegacyPresetCollection>(LegacyPresetsFileName, ct);
        var globalPresets = legacyCollection?.Presets ?? new List<EncryptionPreset>();

        // Copy global presets into every existing profile (with new Guids to avoid collisions)
        foreach (var profile in _profileCollection!.Profiles)
        {
            profile.EncryptionPresets ??= new List<EncryptionPreset>();

            foreach (var preset in globalPresets)
            {
                profile.EncryptionPresets.Add(new EncryptionPreset
                {
                    Id = Guid.NewGuid(),
                    Name = preset.Name,
                    Settings = preset.Settings.Clone(),
                    CreatedAtUtc = preset.CreatedAtUtc
                });
            }
        }

        _profileCollection.SchemaVersion = 2;
        _profileCollection.LastModifiedUtc = DateTime.UtcNow;
        await SaveProfileCollectionAsync(ct);

        // Clean up legacy file
        if (legacyCollection != null)
        {
            await _storage.DeleteProtectedFileAsync(LegacyPresetsFileName, ct);
        }
    }

    private void EnsureInitialized()
    {
        if (_profileCollection == null)
            throw new InvalidOperationException("ProfileService not initialized. Call InitializeAsync first.");
    }

    /// <summary>
    /// Legacy format used only for migration from global presets (schema v1 → v2).
    /// </summary>
    private class LegacyPresetCollection
    {
        public List<EncryptionPreset> Presets { get; set; } = new();
    }
}
