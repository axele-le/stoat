using System.Security.Cryptography;
using System.Text;
using Stoat.Services.Enums;
using Stoat.Services.Interfaces;
using Stoat.Services.Models;

namespace Stoat.Services.Services;

/// <summary>
/// Service for managing CSR storage with DPAPI protection.
/// CSRs are isolated per profile (multi-tenancy).
/// </summary>
public class CsrStorageService : ICsrStorageService
{
    private readonly ISecureStorageService _storage;
    private readonly IProfileService _profileService;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private const string CsrsFileName = "csrs.dat";

    private CsrCollection? _csrCollection;

    /// <inheritdoc />
    public event EventHandler<CsrKeyPair>? CsrCreated;

    /// <inheritdoc />
    public event EventHandler<Guid>? CsrDeleted;

    public CsrStorageService(ISecureStorageService storage, IProfileService profileService)
    {
        _storage = storage;
        _profileService = profileService;
    }

    private Guid GetActiveProfileId()
    {
        var profile = _profileService.ActiveProfile
            ?? throw new InvalidOperationException("No active profile. Cannot perform CSR operations without an active profile.");
        return profile.Id;
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _csrCollection = await _storage.LoadProtectedFileAsync<CsrCollection>(CsrsFileName, ct);

            if (_csrCollection == null)
            {
                // First run - create empty collection
                _csrCollection = new CsrCollection
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
    public async Task<IReadOnlyList<CsrKeyPair>> GetAllCsrsAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profileId = GetActiveProfileId();

            // Filter by active profile
            return _csrCollection!.CsrKeyPairs
                .Where(c => c.ProfileId == profileId)
                .ToList()
                .AsReadOnly();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<CsrKeyPair?> GetCsrByIdAsync(Guid id, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profileId = GetActiveProfileId();

            // Only return if it belongs to the active profile
            return _csrCollection!.CsrKeyPairs.FirstOrDefault(c => c.Id == id && c.ProfileId == profileId);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<CsrKeyPair> SaveCsrAsync(
        string name,
        string csrPem,
        string privateKeyPem,
        string publicKeyPem,
        CsrData data,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("CSR name cannot be empty.", nameof(name));

        if (string.IsNullOrWhiteSpace(csrPem))
            throw new ArgumentException("CSR PEM cannot be empty.", nameof(csrPem));

        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profileId = GetActiveProfileId();

            var csrKeyPair = new CsrKeyPair
            {
                ProfileId = profileId,
                Name = name.Trim(),
                CsrPem = csrPem,
                ProtectedPrivateKey = _storage.Protect(privateKeyPem),
                PublicKey = publicKeyPem,
                KeyAlgorithm = data.KeyAlgorithm,
                SignatureAlgorithm = data.SignatureAlgorithm,
                Algorithm = GetAlgorithmDisplayString(data.KeyAlgorithm),
                CommonName = data.CommonName,
                Organization = data.Organization,
                SanCount = data.SubjectAlternativeNames.Count,
                SubjectDN = BuildSubjectDNString(data),
                CreatedAtUtc = DateTime.UtcNow
            };

            _csrCollection!.CsrKeyPairs.Insert(0, csrKeyPair); // Insert at beginning for newest first
            _csrCollection.LastModifiedUtc = DateTime.UtcNow;

            await SaveCollectionAsync(ct);

            CsrCreated?.Invoke(this, csrKeyPair);

            return csrKeyPair;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<CsrKeyPair> ImportCsrAsync(
        string name,
        string csrPem,
        string? privateKeyPem,
        string publicKeyPem,
        CsrKeyAlgorithm keyAlgorithm,
        CsrSignatureAlgorithm? signatureAlgorithm,
        CsrData parsedData,
        int sanCount,
        List<string> sanList,
        string? sourceFileName,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("CSR name cannot be empty.", nameof(name));

        if (string.IsNullOrWhiteSpace(csrPem))
            throw new ArgumentException("CSR PEM cannot be empty.", nameof(csrPem));

        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profileId = GetActiveProfileId();

            var csrKeyPair = new CsrKeyPair
            {
                ProfileId = profileId,
                Name = name.Trim(),
                CsrPem = csrPem,
                ProtectedPrivateKey = !string.IsNullOrWhiteSpace(privateKeyPem)
                    ? _storage.Protect(privateKeyPem)
                    : null,
                PublicKey = publicKeyPem,
                KeyAlgorithm = keyAlgorithm,
                SignatureAlgorithm = signatureAlgorithm ?? CsrSignatureAlgorithm.SHA256WithRSA,
                Algorithm = GetAlgorithmDisplayString(keyAlgorithm),
                CommonName = parsedData.CommonName,
                Organization = parsedData.Organization,
                OrganizationalUnit = parsedData.OrganizationalUnit,
                Locality = parsedData.Locality,
                State = parsedData.State,
                Country = parsedData.Country,
                SanCount = sanCount,
                SanList = sanList,
                SubjectDN = BuildSubjectDNString(parsedData),
                CreatedAtUtc = DateTime.UtcNow,
                Origin = CsrOrigin.Imported,
                ImportSource = sourceFileName,
                ImportedAtUtc = DateTime.UtcNow
            };

            _csrCollection!.CsrKeyPairs.Insert(0, csrKeyPair);
            _csrCollection.LastModifiedUtc = DateTime.UtcNow;

            await SaveCollectionAsync(ct);

            CsrCreated?.Invoke(this, csrKeyPair);

            return csrKeyPair;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task DeleteCsrAsync(Guid id, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profileId = GetActiveProfileId();

            var csr = _csrCollection!.CsrKeyPairs.FirstOrDefault(c => c.Id == id && c.ProfileId == profileId);
            if (csr == null)
                throw new KeyNotFoundException($"CSR with ID {id} not found in the current profile.");

            _csrCollection.CsrKeyPairs.Remove(csr);
            _csrCollection.LastModifiedUtc = DateTime.UtcNow;

            await SaveCollectionAsync(ct);

            CsrDeleted?.Invoke(this, id);
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

            var csr = _csrCollection!.CsrKeyPairs.FirstOrDefault(c => c.Id == id && c.ProfileId == profileId);
            if (csr == null)
                throw new KeyNotFoundException($"CSR with ID {id} not found in the current profile.");

            if (string.IsNullOrEmpty(csr.ProtectedPrivateKey))
                throw new InvalidOperationException("This CSR does not have a private key.");

            return _storage.Unprotect(csr.ProtectedPrivateKey);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpdateKeyGroupNameAsync(string publicKeyHash, string? groupName, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            EnsureInitialized();
            var profileId = GetActiveProfileId();

            // Find all CSRs with matching public key hash in the active profile
            var matchingCsrs = _csrCollection!.CsrKeyPairs
                .Where(c => c.ProfileId == profileId && ComputePublicKeyHash(c.PublicKey) == publicKeyHash)
                .ToList();

            if (matchingCsrs.Count == 0)
                return;

            // Update the group name for all matching CSRs
            var trimmedName = string.IsNullOrWhiteSpace(groupName) ? null : groupName.Trim();
            foreach (var csr in matchingCsrs)
            {
                csr.KeyGroupName = trimmedName;
            }

            _csrCollection.LastModifiedUtc = DateTime.UtcNow;
            await SaveCollectionAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task SaveCollectionAsync(CancellationToken ct)
    {
        await _storage.SaveProtectedFileAsync(CsrsFileName, _csrCollection!, ct);
    }

    private void EnsureInitialized()
    {
        if (_csrCollection == null)
            throw new InvalidOperationException("CsrStorageService not initialized. Call InitializeAsync first.");
    }

    private static string GetAlgorithmDisplayString(CsrKeyAlgorithm algorithm)
    {
        return algorithm switch
        {
            CsrKeyAlgorithm.RSA_2048 => "RSA-2048",
            CsrKeyAlgorithm.RSA_3072 => "RSA-3072",
            CsrKeyAlgorithm.RSA_4096 => "RSA-4096",
            CsrKeyAlgorithm.ECDSA_P256 => "ECDSA-P256",
            CsrKeyAlgorithm.ECDSA_P384 => "ECDSA-P384",
            CsrKeyAlgorithm.ECDSA_P521 => "ECDSA-P521",
            _ => algorithm.ToString()
        };
    }

    private static string BuildSubjectDNString(CsrData data)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(data.Country))
            parts.Add($"C={data.Country}");
        if (!string.IsNullOrWhiteSpace(data.State))
            parts.Add($"ST={data.State}");
        if (!string.IsNullOrWhiteSpace(data.Locality))
            parts.Add($"L={data.Locality}");
        if (!string.IsNullOrWhiteSpace(data.Organization))
            parts.Add($"O={data.Organization}");
        if (!string.IsNullOrWhiteSpace(data.OrganizationalUnit))
            parts.Add($"OU={data.OrganizationalUnit}");
        if (!string.IsNullOrWhiteSpace(data.CommonName))
            parts.Add($"CN={data.CommonName}");
        if (!string.IsNullOrWhiteSpace(data.EmailAddress))
            parts.Add($"E={data.EmailAddress}");

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Computes a hash of the public key for grouping CSRs by key pair.
    /// </summary>
    private static string ComputePublicKeyHash(string publicKey)
    {
        var bytes = Encoding.UTF8.GetBytes(publicKey);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash, 0, 16);
    }
}
