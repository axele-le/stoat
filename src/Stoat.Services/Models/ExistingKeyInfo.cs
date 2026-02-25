using Stoat.Services.Enums;

namespace Stoat.Services.Models;

/// <summary>
/// Represents an existing private key that can be reused for creating new CSRs.
/// </summary>
public class ExistingKeyInfo
{
    /// <summary>
    /// Unique identifier for the key (derived from public key hash).
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Display label for the key (e.g., "Chiave #1").
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The key algorithm used (e.g., RSA_2048, ECDSA_P256).
    /// </summary>
    public CsrKeyAlgorithm KeyAlgorithm { get; set; }

    /// <summary>
    /// Display name for the algorithm (e.g., "RSA 2048-bit").
    /// </summary>
    public string AlgorithmDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The ID of a CSR that uses this key (used to retrieve the private key).
    /// </summary>
    public Guid CsrId { get; set; }

    /// <summary>
    /// The public key in PEM format.
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;
}
