using Stoat.Services.Enums;

namespace Stoat.Services.Models;

/// <summary>
/// Represents a Subject Alternative Name (SAN) entry.
/// </summary>
public class SanEntry
{
    /// <summary>
    /// The type of SAN entry (DNS, IP, Email, URI).
    /// </summary>
    public SanType Type { get; set; }

    /// <summary>
    /// The value of the SAN entry.
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
