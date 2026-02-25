namespace Stoat.Services.Enums;

/// <summary>
/// Indicates the origin of a CSR entry.
/// </summary>
public enum CsrOrigin
{
    /// <summary>
    /// CSR was generated within the application.
    /// </summary>
    Generated,

    /// <summary>
    /// CSR was imported from an external file.
    /// </summary>
    Imported
}
