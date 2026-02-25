namespace Stoat.Services.Enums;

/// <summary>
/// Supported signature algorithms for CSR.
/// </summary>
public enum CsrSignatureAlgorithm
{
    /// <summary>
    /// SHA-256 with RSA encryption.
    /// </summary>
    SHA256WithRSA,

    /// <summary>
    /// SHA-384 with RSA encryption.
    /// </summary>
    SHA384WithRSA,

    /// <summary>
    /// SHA-512 with RSA encryption.
    /// </summary>
    SHA512WithRSA,

    /// <summary>
    /// SHA-256 with ECDSA.
    /// </summary>
    SHA256WithECDSA,

    /// <summary>
    /// SHA-384 with ECDSA.
    /// </summary>
    SHA384WithECDSA,

    /// <summary>
    /// SHA-512 with ECDSA.
    /// </summary>
    SHA512WithECDSA,

    /// <summary>
    /// SHA-1 with RSA encryption.
    /// </summary>
    SHA1WithRSA,

    /// <summary>
    /// SHA-1 with ECDSA.
    /// </summary>
    SHA1WithECDSA,

    /// <summary>
    /// SHA3-256 with RSA encryption.
    /// </summary>
    SHA3_256WithRSA,

    /// <summary>
    /// SHA3-384 with RSA encryption.
    /// </summary>
    SHA3_384WithRSA,

    /// <summary>
    /// SHA3-512 with RSA encryption.
    /// </summary>
    SHA3_512WithRSA,

    /// <summary>
    /// SHA3-256 with ECDSA.
    /// </summary>
    SHA3_256WithECDSA,

    /// <summary>
    /// SHA3-384 with ECDSA.
    /// </summary>
    SHA3_384WithECDSA,

    /// <summary>
    /// SHA3-512 with ECDSA.
    /// </summary>
    SHA3_512WithECDSA
}
