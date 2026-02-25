namespace Stoat.Services.Enums;

/// <summary>
/// Supported key algorithms for CSR generation.
/// </summary>
public enum CsrKeyAlgorithm
{
    /// <summary>
    /// RSA 2048-bit key.
    /// </summary>
    RSA_2048,

    /// <summary>
    /// RSA 3072-bit key.
    /// </summary>
    RSA_3072,

    /// <summary>
    /// RSA 4096-bit key.
    /// </summary>
    RSA_4096,

    /// <summary>
    /// ECDSA with P-256 curve (secp256r1).
    /// </summary>
    ECDSA_P256,

    /// <summary>
    /// ECDSA with P-384 curve (secp384r1).
    /// </summary>
    ECDSA_P384,

    /// <summary>
    /// ECDSA with P-521 curve (secp521r1).
    /// </summary>
    ECDSA_P521
}
