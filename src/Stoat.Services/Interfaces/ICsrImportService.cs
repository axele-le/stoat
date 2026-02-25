using Stoat.Services.Enums;
using Stoat.Services.Models;

namespace Stoat.Services.Interfaces;

/// <summary>
/// Service for importing CSR and private key files from external sources.
/// </summary>
public interface ICsrImportService
{
    /// <summary>
    /// Parses a CSR file and extracts its information.
    /// Supports PEM format with extensions: .csr, .pem
    /// </summary>
    /// <param name="filePath">Path to the CSR file.</param>
    /// <returns>Import result with parsed CSR data.</returns>
    Task<CsrImportResult> ParseCsrFileAsync(string filePath);

    /// <summary>
    /// Parses a private key file.
    /// Supports PEM format with extensions: .key, .pem
    /// Supports both unencrypted and password-protected keys.
    /// </summary>
    /// <param name="filePath">Path to the private key file.</param>
    /// <param name="password">Optional password for encrypted keys.</param>
    /// <returns>Import result with parsed key data.</returns>
    Task<CsrImportResult> ParsePrivateKeyFileAsync(string filePath, string? password = null);

    /// <summary>
    /// Validates that a CSR and private key match (same public key).
    /// </summary>
    /// <param name="csrPem">The CSR in PEM format.</param>
    /// <param name="privateKeyPem">The private key in PEM format.</param>
    /// <returns>True if the key pair matches.</returns>
    bool ValidateKeyPairMatch(string csrPem, string privateKeyPem);

    /// <summary>
    /// Detects the key algorithm from a PEM-encoded CSR or key.
    /// </summary>
    /// <param name="pem">PEM-encoded content.</param>
    /// <returns>The detected algorithm, or null if unsupported/invalid.</returns>
    CsrKeyAlgorithm? DetectKeyAlgorithm(string pem);

    /// <summary>
    /// Detects the signature algorithm from a PEM-encoded CSR.
    /// </summary>
    /// <param name="csrPem">PEM-encoded CSR.</param>
    /// <returns>The detected signature algorithm, or null if unsupported/invalid.</returns>
    CsrSignatureAlgorithm? DetectSignatureAlgorithm(string csrPem);

    /// <summary>
    /// Extracts the public key PEM from a CSR.
    /// </summary>
    /// <param name="csrPem">PEM-encoded CSR.</param>
    /// <returns>Public key in PEM format, or null on failure.</returns>
    string? ExtractPublicKeyFromCsr(string csrPem);

    /// <summary>
    /// Derives the public key PEM from a private key.
    /// </summary>
    /// <param name="privateKeyPem">PEM-encoded private key.</param>
    /// <returns>Public key in PEM format, or null on failure.</returns>
    string? DerivePublicKeyFromPrivateKey(string privateKeyPem);
}
