using System.Text.Json.Serialization;
using Stoat.Services.Enums;

namespace Stoat.Services.Models;

/// <summary>
/// Represents a generated CSR with its associated key pair.
/// </summary>
public class CsrKeyPair
{
    /// <summary>
    /// Unique identifier for the CSR.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The profile this CSR belongs to (multi-tenancy support).
    /// </summary>
    public Guid ProfileId { get; set; }

    /// <summary>
    /// Human-readable name for the CSR.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The CSR in PEM format.
    /// </summary>
    public string CsrPem { get; set; } = string.Empty;

    /// <summary>
    /// The private key in PEM format (stored protected with DPAPI).
    /// </summary>
    public string? ProtectedPrivateKey { get; set; }

    /// <summary>
    /// The public key in PEM format (stored in plaintext).
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Custom name for the key group this CSR belongs to.
    /// All CSRs sharing the same public key should have the same KeyGroupName.
    /// </summary>
    public string? KeyGroupName { get; set; }

    /// <summary>
    /// Key algorithm used.
    /// </summary>
    public CsrKeyAlgorithm KeyAlgorithm { get; set; }

    /// <summary>
    /// Signature algorithm used.
    /// </summary>
    public CsrSignatureAlgorithm SignatureAlgorithm { get; set; }

    /// <summary>
    /// Algorithm display string (e.g., "RSA-2048", "ECDSA-P256").
    /// </summary>
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>
    /// The Common Name (CN) from the subject.
    /// </summary>
    public string CommonName { get; set; } = string.Empty;

    /// <summary>
    /// The Organization (O) from the subject.
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// The Organizational Unit (OU) from the subject.
    /// </summary>
    public string? OrganizationalUnit { get; set; }

    /// <summary>
    /// The Locality (L) from the subject.
    /// </summary>
    public string? Locality { get; set; }

    /// <summary>
    /// The State/Province (ST) from the subject.
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// The Country (C) from the subject.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Subject Alternative Names count.
    /// </summary>
    public int SanCount { get; set; }

    /// <summary>
    /// List of SANs as formatted strings (e.g., "DNS: example.com").
    /// </summary>
    public List<string> SanList { get; set; } = new();

    /// <summary>
    /// UTC timestamp when the CSR was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Version number for optimistic concurrency and migration support.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Indicates whether this CSR has a private key.
    /// </summary>
    public bool HasPrivateKey => !string.IsNullOrEmpty(ProtectedPrivateKey);

    /// <summary>
    /// Full subject DN string for display.
    /// </summary>
    public string SubjectDN { get; set; } = string.Empty;

    /// <summary>
    /// Origin of the CSR (Generated or Imported).
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CsrOrigin Origin { get; set; } = CsrOrigin.Generated;

    /// <summary>
    /// Source file name when imported (e.g., "server.csr").
    /// </summary>
    public string? ImportSource { get; set; }

    /// <summary>
    /// UTC timestamp when the CSR was imported (null for generated CSRs).
    /// </summary>
    public DateTime? ImportedAtUtc { get; set; }
}
