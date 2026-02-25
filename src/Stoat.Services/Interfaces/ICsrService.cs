using Stoat.Services.Enums;
using Stoat.Services.Models;

namespace Stoat.Services.Interfaces;

/// <summary>
/// Service for generating and validating Certificate Signing Requests (CSRs).
/// </summary>
public interface ICsrService
{
    /// <summary>
    /// Generates a new CSR with a new key pair.
    /// </summary>
    /// <param name="data">The CSR data containing subject, key algorithm, and extensions.</param>
    /// <returns>A tuple containing the CSR PEM, private key PEM, and public key PEM.</returns>
    (string CsrPem, string PrivateKeyPem, string PublicKeyPem) GenerateCsr(CsrData data);

    /// <summary>
    /// Generates a new CSR using an existing private key.
    /// </summary>
    /// <param name="data">The CSR data containing subject and extensions.</param>
    /// <param name="privateKeyPem">The existing private key in PEM format.</param>
    /// <returns>A tuple containing the CSR PEM and public key PEM (private key is the same as provided).</returns>
    (string CsrPem, string PublicKeyPem) GenerateCsrWithExistingKey(CsrData data, string privateKeyPem);

    /// <summary>
    /// Validates a CSR PEM string.
    /// </summary>
    /// <param name="csrPem">The CSR in PEM format.</param>
    /// <returns>True if the CSR is valid, false otherwise.</returns>
    bool ValidateCsr(string csrPem);

    /// <summary>
    /// Parses a CSR and extracts its information.
    /// </summary>
    /// <param name="csrPem">The CSR in PEM format.</param>
    /// <returns>The parsed CSR data, or null if parsing fails.</returns>
    CsrData? ParseCsr(string csrPem);

    /// <summary>
    /// Gets compatible signature algorithms for a given key algorithm.
    /// </summary>
    /// <param name="keyAlgorithm">The key algorithm.</param>
    /// <returns>List of compatible signature algorithms.</returns>
    IReadOnlyList<CsrSignatureAlgorithm> GetCompatibleSignatureAlgorithms(CsrKeyAlgorithm keyAlgorithm);

    /// <summary>
    /// Gets the default CSR data for a preset type.
    /// </summary>
    /// <param name="preset">The preset type.</param>
    /// <returns>Pre-configured CSR data for the preset.</returns>
    CsrData GetPresetDefaults(CsrPresetType preset);

    /// <summary>
    /// Gets a human-readable display name for a key algorithm.
    /// </summary>
    /// <param name="algorithm">The key algorithm.</param>
    /// <returns>Display name (e.g., "RSA 2048-bit").</returns>
    string GetKeyAlgorithmDisplayName(CsrKeyAlgorithm algorithm);

    /// <summary>
    /// Gets a human-readable display name for a signature algorithm.
    /// </summary>
    /// <param name="algorithm">The signature algorithm.</param>
    /// <returns>Display name (e.g., "SHA-256 with RSA").</returns>
    string GetSignatureAlgorithmDisplayName(CsrSignatureAlgorithm algorithm);
}
