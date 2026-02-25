namespace Stoat.Services.Enums;

/// <summary>
/// Common Extended Key Usage OIDs.
/// </summary>
public enum ExtendedKeyUsageOid
{
    /// <summary>
    /// TLS Web Server Authentication (1.3.6.1.5.5.7.3.1).
    /// </summary>
    ServerAuth,

    /// <summary>
    /// TLS Web Client Authentication (1.3.6.1.5.5.7.3.2).
    /// </summary>
    ClientAuth,

    /// <summary>
    /// Code Signing (1.3.6.1.5.5.7.3.3).
    /// </summary>
    CodeSigning,

    /// <summary>
    /// Email Protection / S/MIME (1.3.6.1.5.5.7.3.4).
    /// </summary>
    EmailProtection,

    /// <summary>
    /// Time Stamping (1.3.6.1.5.5.7.3.8).
    /// </summary>
    TimeStamping,

    /// <summary>
    /// OCSP Signing (1.3.6.1.5.5.7.3.9).
    /// </summary>
    OcspSigning
}
