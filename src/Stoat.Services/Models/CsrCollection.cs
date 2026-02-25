namespace Stoat.Services.Models;

/// <summary>
/// Container for all CSR key pairs with metadata about the collection.
/// </summary>
public class CsrCollection
{
    /// <summary>
    /// Schema version for future migrations.
    /// </summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>
    /// Collection of all CSR key pairs.
    /// </summary>
    public List<CsrKeyPair> CsrKeyPairs { get; set; } = new();

    /// <summary>
    /// UTC timestamp of last modification to the collection.
    /// </summary>
    public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Unique identifier for this installation (used for Data Protection key isolation).
    /// </summary>
    public string? InstallationId { get; set; }
}
