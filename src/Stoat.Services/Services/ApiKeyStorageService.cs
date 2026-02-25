using Stoat.Services.Interfaces;
using Stoat.Services.Models;

namespace Stoat.Services.Services;

/// <summary>
/// Service for managing API key storage with DPAPI protection.
/// Keys are isolated per profile (multi-tenancy).
/// </summary>
public class ApiKeyStorageService : IApiKeyStorageService
{
    private readonly ISecureStorageService _storage;
    private readonly IApiKeyService _apiKeyService;
    private readonly IProfileService _profileService;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private const string ApiKeysFileName = "apikeys.dat";
    private const int DefaultKeyLength = 32;

    private ApiKeyCollection? _keyCollection;

    /// <inheritdoc />
    public event EventHandler<ApiKey>? KeyCreated;

    /// <inheritdoc />
    public event EventHandler<Guid>? KeyDeleted;

    public ApiKeyStorageService(ISecureStorageService storage, IApiKeyService apiKeyService, IProfileService profileService)
    {
        _storage = storage;
        _apiKeyService = apiKeyService;
        _profileService = profileService;
    }

    private Guid GetActiveProfileId()
    {
        var profile = _profileService.ActiveProfile
            ?? throw new InvalidOperationException("No active profile. Cannot perform API key operations without an active profile.");
        return profile.Id;
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _keyCollection = await _storage.LoadProtectedFileAsync<ApiKeyCollection>(ApiKeysFileName, ct);

            if (_keyCollection == null)
            {
                // First run - create empty collection
                _keyCollection = new ApiKeyCollection
                {
                    InstallationId = Guid.NewGuid().ToString()
                };
                await SaveCollectionAsync(ct);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApiKey>> GetAllKeysAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profileId = GetActiveProfileId();

            // Filter by active profile
            return _keyCollection!.Keys
                .Where(k => k.ProfileId == profileId)
                .ToList()
                .AsReadOnly();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ApiKey?> GetKeyByIdAsync(Guid id, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profileId = GetActiveProfileId();

            // Only return if it belongs to the active profile
            return _keyCollection!.Keys.FirstOrDefault(k => k.Id == id && k.ProfileId == profileId);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ApiKey> CreateKeyAsync(string name, ApiKeyType type, int length = 32, ApiKeyComplexity complexity = ApiKeyComplexity.AlphanumericSymbols, string? prefix = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("API key name cannot be empty.", nameof(name));

        if (length < 8 || length > 256)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be between 8 and 256.");

        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profileId = GetActiveProfileId();

            // Use provided prefix or empty string
            var actualPrefix = prefix ?? string.Empty;

            // Generate the key value based on complexity
            var keyValue = actualPrefix + GenerateKeyFromComplexity(length, complexity);

            var apiKey = new ApiKey
            {
                ProfileId = profileId,
                Name = name.Trim(),
                Type = type,
                Length = length,
                Complexity = complexity,
                Prefix = actualPrefix,
                ProtectedValue = _storage.Protect(keyValue),
                CreatedAtUtc = DateTime.UtcNow
            };

            _keyCollection!.Keys.Insert(0, apiKey); // Insert at beginning for newest first
            _keyCollection.LastModifiedUtc = DateTime.UtcNow;

            await SaveCollectionAsync(ct);

            KeyCreated?.Invoke(this, apiKey);

            return apiKey;
        }
        finally
        {
            _lock.Release();
        }
    }

    private string GenerateKeyFromComplexity(int length, ApiKeyComplexity complexity)
    {
        // Base64UrlSafe uses a different generation method
        if (complexity == ApiKeyComplexity.Base64UrlSafe)
        {
            return GenerateBase64UrlSafeKey(length);
        }

        var charset = complexity switch
        {
            ApiKeyComplexity.Numeric => "0123456789",
            ApiKeyComplexity.AlphanumericLower => "abcdefghijklmnopqrstuvwxyz0123456789",
            ApiKeyComplexity.Alphanumeric => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789",
            ApiKeyComplexity.AlphanumericSymbols => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()-_=+[]{}|;:,.<>?",
            _ => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
        };

        var bytes = new byte[length];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);

        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = charset[bytes[i] % charset.Length];
        }
        return new string(result);
    }

    private string GenerateBase64UrlSafeKey(int length)
    {
        // Calculate bytes needed to get approximately the desired length after Base64 encoding
        // Base64 produces 4 chars for every 3 bytes, so we need roughly (length * 3 / 4) bytes
        var bytesNeeded = (int)Math.Ceiling(length * 3.0 / 4.0);
        var bytes = new byte[bytesNeeded];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);

        // Convert to Base64 URL-safe (replace + with -, / with _, remove padding =)
        var base64 = Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        // Trim or pad to exact length requested
        if (base64.Length >= length)
        {
            return base64[..length];
        }

        // If somehow shorter (shouldn't happen), generate more
        return base64.PadRight(length, 'A');
    }

    /// <inheritdoc />
    public async Task DeleteKeyAsync(Guid id, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profileId = GetActiveProfileId();

            var apiKey = _keyCollection!.Keys.FirstOrDefault(k => k.Id == id && k.ProfileId == profileId);
            if (apiKey == null)
                throw new KeyNotFoundException($"API key with ID {id} not found in the current profile.");

            _keyCollection.Keys.Remove(apiKey);
            _keyCollection.LastModifiedUtc = DateTime.UtcNow;

            await SaveCollectionAsync(ct);

            KeyDeleted?.Invoke(this, id);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<string> GetDecryptedValueAsync(Guid id, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profileId = GetActiveProfileId();

            var apiKey = _keyCollection!.Keys.FirstOrDefault(k => k.Id == id && k.ProfileId == profileId);
            if (apiKey == null)
                throw new KeyNotFoundException($"API key with ID {id} not found in the current profile.");

            return _storage.Unprotect(apiKey.ProtectedValue);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task SaveCollectionAsync(CancellationToken ct)
    {
        await _storage.SaveProtectedFileAsync(ApiKeysFileName, _keyCollection!, ct);
    }

    private void EnsureInitialized()
    {
        if (_keyCollection == null)
            throw new InvalidOperationException("ApiKeyStorageService not initialized. Call InitializeAsync first.");
    }
}
