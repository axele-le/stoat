namespace Stoat.Services.Models;

/// <summary>
/// Represents a PEM key pair with DPAPI protection for the private key.
/// </summary>
public class PemKeyPair
{
    /// <summary>
    /// Unique identifier for the key pair.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The profile this key pair belongs to (multi-tenancy support).
    /// </summary>
    public Guid ProfileId { get; set; }

    /// <summary>
    /// Human-readable name for the key pair.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Key size in bits (2048, 3072, or 4096).
    /// </summary>
    public int KeySizeBits { get; set; }

    /// <summary>
    /// Algorithm identifier (e.g., "RSA-2048", "RSA-3072", "RSA-4096").
    /// </summary>
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>
    /// The private key in PEM format (stored protected with DPAPI).
    /// Null if only public key was imported.
    /// </summary>
    public string? ProtectedPrivateKey { get; set; }

    /// <summary>
    /// The public key in PEM format (stored in plaintext - public keys are safe).
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the key pair was created or imported.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Version number for optimistic concurrency and migration support.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Indicates whether this key pair has a private key.
    /// </summary>
    public bool HasPrivateKey => !string.IsNullOrEmpty(ProtectedPrivateKey);
}
