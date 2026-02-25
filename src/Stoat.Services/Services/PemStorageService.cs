using System.Security.Cryptography;
using Stoat.Services.Interfaces;
using Stoat.Services.Models;

namespace Stoat.Services.Services;

/// <summary>
/// Service for managing PEM key pair storage with DPAPI protection.
/// Keys are isolated per profile (multi-tenancy).
/// </summary>
public class PemStorageService : IPemStorageService
{
    private readonly ISecureStorageService _storage;
    private readonly IPemService _pemService;
    private readonly IProfileService _profileService;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private const string PemsFileName = "pems.dat";

    private PemKeyCollection? _keyCollection;

    /// <inheritdoc />
    public event EventHandler<PemKeyPair>? KeyPairCreated;

    /// <inheritdoc />
    public event EventHandler<Guid>? KeyPairDeleted;

    public PemStorageService(ISecureStorageService storage, IPemService pemService, IProfileService profileService)
    {
        _storage = storage;
        _pemService = pemService;
        _profileService = profileService;
    }

    private Guid GetActiveProfileId()
    {
        var profile = _profileService.ActiveProfile
            ?? throw new InvalidOperationException("No active profile. Cannot perform PEM operations without an active profile.");
        return profile.Id;
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _keyCollection = await _storage.LoadProtectedFileAsync<PemKeyCollection>(PemsFileName, ct);

            if (_keyCollection == null)
            {
                // First run - create empty collection
                _keyCollection = new PemKeyCollection
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
    public async Task<IReadOnlyList<PemKeyPair>> GetAllKeyPairsAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profileId = GetActiveProfileId();

            // Filter by active profile
            return _keyCollection!.KeyPairs
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
    public async Task<PemKeyPair?> GetKeyPairByIdAsync(Guid id, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profileId = GetActiveProfileId();

            // Only return if it belongs to the active profile
            return _keyCollection!.KeyPairs.FirstOrDefault(k => k.Id == id && k.ProfileId == profileId);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<PemKeyPair> CreateKeyPairAsync(string name, int keySize, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Key pair name cannot be empty.", nameof(name));

        if (keySize != 2048 && keySize != 3072 && keySize != 4096)
            throw new ArgumentOutOfRangeException(nameof(keySize), "Key size must be 2048, 3072, or 4096 bits.");

        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profileId = GetActiveProfileId();

            // Generate key pair
            var pems = await Task.Run(() => _pemService.GenerateKeyPair(keySize), ct);

            var keyPair = new PemKeyPair
            {
                ProfileId = profileId,
                Name = name.Trim(),
                KeySizeBits = keySize,
                Algorithm = $"RSA-{keySize}",
                ProtectedPrivateKey = _storage.Protect(pems.PrivateKey!),
                PublicKey = pems.PublicKey!,
                CreatedAtUtc = DateTime.UtcNow
            };

            _keyCollection!.KeyPairs.Insert(0, keyPair); // Insert at beginning for newest first
            _keyCollection.LastModifiedUtc = DateTime.UtcNow;

            await SaveCollectionAsync(ct);

            KeyPairCreated?.Invoke(this, keyPair);

            return keyPair;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<PemKeyPair> ImportKeyPairAsync(string name, string privateKeyPem, string publicKeyPem, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Key pair name cannot be empty.", nameof(name));

        if (string.IsNullOrWhiteSpace(privateKeyPem))
            throw new ArgumentException("Private key cannot be empty.", nameof(privateKeyPem));

        if (string.IsNullOrWhiteSpace(publicKeyPem))
            throw new ArgumentException("Public key cannot be empty.", nameof(publicKeyPem));

        // Validate keys
        if (!_pemService.ValidatePrivateKey(privateKeyPem))
            throw new ArgumentException("Invalid private key format.", nameof(privateKeyPem));

        if (!_pemService.ValidatePublicKey(publicKeyPem))
            throw new ArgumentException("Invalid public key format.", nameof(publicKeyPem));

        // Determine key size from private key
        int keySize = GetKeySizeFromPem(privateKeyPem);

        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profileId = GetActiveProfileId();

            var keyPair = new PemKeyPair
            {
                ProfileId = profileId,
                Name = name.Trim(),
                KeySizeBits = keySize,
                Algorithm = $"RSA-{keySize}",
                ProtectedPrivateKey = _storage.Protect(privateKeyPem),
                PublicKey = publicKeyPem,
                CreatedAtUtc = DateTime.UtcNow
            };

            _keyCollection!.KeyPairs.Insert(0, keyPair);
            _keyCollection.LastModifiedUtc = DateTime.UtcNow;

            await SaveCollectionAsync(ct);

            KeyPairCreated?.Invoke(this, keyPair);

            return keyPair;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<PemKeyPair> ImportPublicKeyOnlyAsync(string name, string publicKeyPem, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Key name cannot be empty.", nameof(name));

        if (string.IsNullOrWhiteSpace(publicKeyPem))
            throw new ArgumentException("Public key cannot be empty.", nameof(publicKeyPem));

        // Validate public key
        if (!_pemService.ValidatePublicKey(publicKeyPem))
            throw new ArgumentException("Invalid public key format.", nameof(publicKeyPem));

        // Determine key size from public key
        int keySize = GetKeySizeFromPem(publicKeyPem);

        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profileId = GetActiveProfileId();

            var keyPair = new PemKeyPair
            {
                ProfileId = profileId,
                Name = name.Trim(),
                KeySizeBits = keySize,
                Algorithm = $"RSA-{keySize}",
                ProtectedPrivateKey = null, // No private key
                PublicKey = publicKeyPem,
                CreatedAtUtc = DateTime.UtcNow
            };

            _keyCollection!.KeyPairs.Insert(0, keyPair);
            _keyCollection.LastModifiedUtc = DateTime.UtcNow;

            await SaveCollectionAsync(ct);

            KeyPairCreated?.Invoke(this, keyPair);

            return keyPair;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task DeleteKeyPairAsync(Guid id, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profileId = GetActiveProfileId();

            var keyPair = _keyCollection!.KeyPairs.FirstOrDefault(k => k.Id == id && k.ProfileId == profileId);
            if (keyPair == null)
                throw new KeyNotFoundException($"Key pair with ID {id} not found in the current profile.");

            _keyCollection.KeyPairs.Remove(keyPair);
            _keyCollection.LastModifiedUtc = DateTime.UtcNow;

            await SaveCollectionAsync(ct);

            KeyPairDeleted?.Invoke(this, id);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<string> GetDecryptedPrivateKeyAsync(Guid id, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profileId = GetActiveProfileId();

            var keyPair = _keyCollection!.KeyPairs.FirstOrDefault(k => k.Id == id && k.ProfileId == profileId);
            if (keyPair == null)
                throw new KeyNotFoundException($"Key pair with ID {id} not found in the current profile.");

            if (string.IsNullOrEmpty(keyPair.ProtectedPrivateKey))
                throw new InvalidOperationException("This key pair does not have a private key.");

            return _storage.Unprotect(keyPair.ProtectedPrivateKey);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task SaveCollectionAsync(CancellationToken ct)
    {
        await _storage.SaveProtectedFileAsync(PemsFileName, _keyCollection!, ct);
    }

    private void EnsureInitialized()
    {
        if (_keyCollection == null)
            throw new InvalidOperationException("PemStorageService not initialized. Call InitializeAsync first.");
    }

    private static int GetKeySizeFromPem(string pem)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            return rsa.KeySize;
        }
        catch
        {
            // Default to 2048 if we can't determine
            return 2048;
        }
    }
}
