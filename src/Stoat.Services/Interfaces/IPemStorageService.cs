using Stoat.Services.Models;

namespace Stoat.Services.Interfaces;

/// <summary>
/// Service for managing PEM key pair storage with DPAPI protection.
/// </summary>
public interface IPemStorageService
{
    /// <summary>
    /// Initializes the service by loading existing key pairs from storage.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all stored PEM key pairs.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of PEM key pairs.</returns>
    Task<IReadOnlyList<PemKeyPair>> GetAllKeyPairsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a specific PEM key pair by its ID.
    /// </summary>
    /// <param name="id">The key pair ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The key pair if found, null otherwise.</returns>
    Task<PemKeyPair?> GetKeyPairByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates and stores a new RSA key pair.
    /// </summary>
    /// <param name="name">Human-readable name for the key pair.</param>
    /// <param name="keySize">Key size in bits (2048, 3072, or 4096).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created key pair.</returns>
    Task<PemKeyPair> CreateKeyPairAsync(string name, int keySize, CancellationToken ct = default);

    /// <summary>
    /// Imports an existing PEM key pair.
    /// </summary>
    /// <param name="name">Human-readable name for the key pair.</param>
    /// <param name="privateKeyPem">The private key in PEM format.</param>
    /// <param name="publicKeyPem">The public key in PEM format.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The imported key pair.</returns>
    Task<PemKeyPair> ImportKeyPairAsync(string name, string privateKeyPem, string publicKeyPem, CancellationToken ct = default);

    /// <summary>
    /// Imports only a public key (no private key).
    /// </summary>
    /// <param name="name">Human-readable name for the key.</param>
    /// <param name="publicKeyPem">The public key in PEM format.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The imported key pair (with no private key).</returns>
    Task<PemKeyPair> ImportPublicKeyOnlyAsync(string name, string publicKeyPem, CancellationToken ct = default);

    /// <summary>
    /// Deletes a PEM key pair by its ID.
    /// </summary>
    /// <param name="id">The key pair ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteKeyPairAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets the decrypted private key for a key pair.
    /// </summary>
    /// <param name="id">The key pair ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The decrypted private key in PEM format.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the key pair is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the key pair has no private key.</exception>
    Task<string> GetDecryptedPrivateKeyAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Raised when a new key pair is created or imported.
    /// </summary>
    event EventHandler<PemKeyPair>? KeyPairCreated;

    /// <summary>
    /// Raised when a key pair is deleted.
    /// </summary>
    event EventHandler<Guid>? KeyPairDeleted;
}
