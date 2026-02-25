using Stoat.Services.Enums;

namespace Stoat.Services.Models;

/// <summary>
/// Contains all data needed to generate a Certificate Signing Request (CSR).
/// </summary>
public class CsrData
{
    /// <summary>
    /// Common Name (CN) - typically the domain name for SSL/TLS certificates.
    /// </summary>
    public string CommonName { get; set; } = string.Empty;

    /// <summary>
    /// Organization (O) - the legal name of the organization.
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// Organizational Unit (OU) - the department within the organization.
    /// </summary>
    public string? OrganizationalUnit { get; set; }

    /// <summary>
    /// Country (C) - two-letter ISO 3166-1 alpha-2 country code.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// State/Province (ST) - the state or province name.
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Locality (L) - the city or locality name.
    /// </summary>
    public string? Locality { get; set; }

    /// <summary>
    /// Email Address - contact email for the certificate.
    /// </summary>
    public string? EmailAddress { get; set; }

    /// <summary>
    /// Key algorithm to use for key pair generation.
    /// </summary>
    public CsrKeyAlgorithm KeyAlgorithm { get; set; } = CsrKeyAlgorithm.RSA_2048;

    /// <summary>
    /// Signature algorithm for signing the CSR.
    /// </summary>
    public CsrSignatureAlgorithm SignatureAlgorithm { get; set; } = CsrSignatureAlgorithm.SHA256WithRSA;

    /// <summary>
    /// Subject Alternative Names (SANs).
    /// </summary>
    public List<SanEntry> SubjectAlternativeNames { get; set; } = new();

    /// <summary>
    /// Key Usage extension flags.
    /// </summary>
    public KeyUsageFlags KeyUsage { get; set; } = KeyUsageFlags.None;

    /// <summary>
    /// Extended Key Usage OIDs.
    /// </summary>
    public List<ExtendedKeyUsageOid> ExtendedKeyUsage { get; set; } = new();

    /// <summary>
    /// Whether this certificate can be used as a CA.
    /// </summary>
    public bool IsCA { get; set; } = false;
}
