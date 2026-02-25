using Stoat.Services.Enums;
using Stoat.Services.Models;

namespace Stoat.Services.Interfaces;

/// <summary>
/// Service for managing CSR storage with DPAPI protection.
/// </summary>
public interface ICsrStorageService
{
    /// <summary>
    /// Initializes the service by loading existing CSRs from storage.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all stored CSR key pairs for the active profile.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of CSR key pairs.</returns>
    Task<IReadOnlyList<CsrKeyPair>> GetAllCsrsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a specific CSR by its ID.
    /// </summary>
    /// <param name="id">The CSR ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The CSR if found, null otherwise.</returns>
    Task<CsrKeyPair?> GetCsrByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Saves a new CSR with its key pair.
    /// </summary>
    /// <param name="name">Human-readable name for the CSR.</param>
    /// <param name="csrPem">The CSR in PEM format.</param>
    /// <param name="privateKeyPem">The private key in PEM format.</param>
    /// <param name="publicKeyPem">The public key in PEM format.</param>
    /// <param name="data">The CSR data used for generation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The saved CSR key pair.</returns>
    Task<CsrKeyPair> SaveCsrAsync(
        string name,
        string csrPem,
        string privateKeyPem,
        string publicKeyPem,
        CsrData data,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a CSR by its ID.
    /// </summary>
    /// <param name="id">The CSR ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteCsrAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets the decrypted private key for a CSR.
    /// </summary>
    /// <param name="id">The CSR ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The decrypted private key in PEM format.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the CSR is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the CSR has no private key.</exception>
    Task<string> GetDecryptedPrivateKeyAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Updates the key group name for all CSRs sharing the same public key.
    /// </summary>
    /// <param name="publicKeyHash">The hash of the public key identifying the group.</param>
    /// <param name="groupName">The new group name (can be null to clear).</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateKeyGroupNameAsync(string publicKeyHash, string? groupName, CancellationToken ct = default);

    /// <summary>
    /// Imports an externally created CSR with optional private key.
    /// </summary>
    /// <param name="name">Human-readable name for the imported CSR.</param>
    /// <param name="csrPem">The CSR in PEM format.</param>
    /// <param name="privateKeyPem">The private key in PEM format (null for CSR-only import).</param>
    /// <param name="publicKeyPem">The public key in PEM format.</param>
    /// <param name="keyAlgorithm">The detected key algorithm.</param>
    /// <param name="signatureAlgorithm">The detected signature algorithm (null if unknown).</param>
    /// <param name="parsedData">Parsed subject DN data from the CSR.</param>
    /// <param name="sanCount">Number of SANs in the CSR.</param>
    /// <param name="sanList">List of SANs as formatted strings.</param>
    /// <param name="sourceFileName">Original file name of the imported CSR.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The saved CSR key pair.</returns>
    Task<CsrKeyPair> ImportCsrAsync(
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
        CancellationToken ct = default);

    /// <summary>
    /// Raised when a new CSR is created.
    /// </summary>
    event EventHandler<CsrKeyPair>? CsrCreated;

    /// <summary>
    /// Raised when a CSR is deleted.
    /// </summary>
    event EventHandler<Guid>? CsrDeleted;
}
