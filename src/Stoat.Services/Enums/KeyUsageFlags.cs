namespace Stoat.Services.Enums;

/// <summary>
/// X.509 Key Usage extension flags.
/// </summary>
[Flags]
public enum KeyUsageFlags
{
    /// <summary>
    /// No key usage specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// Digital signature (bit 0).
    /// </summary>
    DigitalSignature = 1 << 0,

    /// <summary>
    /// Non-repudiation / content commitment (bit 1).
    /// </summary>
    NonRepudiation = 1 << 1,

    /// <summary>
    /// Key encipherment (bit 2).
    /// </summary>
    KeyEncipherment = 1 << 2,

    /// <summary>
    /// Data encipherment (bit 3).
    /// </summary>
    DataEncipherment = 1 << 3,

    /// <summary>
    /// Key agreement (bit 4).
    /// </summary>
    KeyAgreement = 1 << 4,

    /// <summary>
    /// Key certificate signing (bit 5).
    /// </summary>
    KeyCertSign = 1 << 5,

    /// <summary>
    /// CRL signing (bit 6).
    /// </summary>
    CrlSign = 1 << 6,

    /// <summary>
    /// Encipher only (bit 7) - used with Key Agreement.
    /// </summary>
    EncipherOnly = 1 << 7,

    /// <summary>
    /// Decipher only (bit 8) - used with Key Agreement.
    /// </summary>
    DecipherOnly = 1 << 8
}
