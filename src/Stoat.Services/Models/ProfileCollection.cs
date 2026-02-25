namespace Stoat.Services.Models;

/// <summary>
/// Container for all profiles with metadata about the collection.
/// </summary>
public class ProfileCollection
{
    /// <summary>
    /// Schema version for future migrations.
    /// </summary>
    public int SchemaVersion { get; set; } = 2;

    /// <summary>
    /// The ID of the currently active profile.
    /// </summary>
    public Guid? ActiveProfileId { get; set; }

    /// <summary>
    /// Collection of all profiles.
    /// </summary>
    public List<Profile> Profiles { get; set; } = new();

    /// <summary>
    /// UTC timestamp of last modification to the collection.
    /// </summary>
    public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Unique identifier for this installation (used for Data Protection key isolation).
    /// </summary>
    public string InstallationId { get; set; } = Guid.NewGuid().ToString();
}
