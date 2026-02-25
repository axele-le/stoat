namespace Stoat.Services.Enums;

/// <summary>
/// Predefined CSR configuration presets for common use cases.
/// </summary>
public enum CsrPresetType
{
    /// <summary>
    /// Web server / TLS server authentication preset.
    /// Key Usage: Digital Signature, Key Encipherment
    /// Extended Key Usage: Server Auth, Client Auth
    /// </summary>
    WebServer,

    /// <summary>
    /// Client authentication preset.
    /// Key Usage: Digital Signature
    /// Extended Key Usage: Client Auth
    /// </summary>
    ClientAuth,

    /// <summary>
    /// Code signing preset.
    /// Key Usage: Digital Signature
    /// Extended Key Usage: Code Signing
    /// </summary>
    CodeSigning,

    /// <summary>
    /// Email / S/MIME preset.
    /// Key Usage: Digital Signature, Key Encipherment
    /// Extended Key Usage: Email Protection
    /// </summary>
    Email
}
