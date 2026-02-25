namespace Stoat.Services.Enums;

/// <summary>
/// Subject Alternative Name (SAN) entry types.
/// </summary>
public enum SanType
{
    /// <summary>
    /// DNS name (e.g., www.example.com, *.example.com).
    /// </summary>
    DNS,

    /// <summary>
    /// IP address (IPv4 or IPv6).
    /// </summary>
    IP,

    /// <summary>
    /// Email address (RFC 822 name).
    /// </summary>
    Email,

    /// <summary>
    /// URI (Uniform Resource Identifier).
    /// </summary>
    URI
}
