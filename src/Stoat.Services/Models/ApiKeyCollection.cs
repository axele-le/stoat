namespace Stoat.Services.Models;

/// <summary>
/// Container for all API keys with metadata about the collection.
/// </summary>
public class ApiKeyCollection
{
    /// <summary>
    /// Schema version for future migrations.
    /// </summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>
    /// Collection of all API keys.
    /// </summary>
    public List<ApiKey> Keys { get; set; } = new();

    /// <summary>
    /// UTC timestamp of last modification to the collection.
    /// </summary>
    public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Unique identifier for this installation (used for Data Protection key isolation).
    /// </summary>
    public string? InstallationId { get; set; }
}
