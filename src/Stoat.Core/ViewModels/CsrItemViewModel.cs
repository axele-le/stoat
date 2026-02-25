using CommunityToolkit.Mvvm.ComponentModel;
using Stoat.Services.Enums;

namespace Stoat.Core.ViewModels;

/// <summary>
/// ViewModel for a single CSR item in the DataGrid.
/// </summary>
public partial class CsrItemViewModel : ObservableObject
{
    /// <summary>
    /// Unique identifier for the CSR.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Human-readable name for the CSR.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The CSR in PEM format.
    /// </summary>
    public string CsrPem { get; set; } = string.Empty;

    /// <summary>
    /// The public key in PEM format.
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Algorithm display string (e.g., "RSA-2048").
    /// </summary>
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>
    /// Key algorithm used.
    /// </summary>
    public CsrKeyAlgorithm KeyAlgorithm { get; set; }

    /// <summary>
    /// Signature algorithm used.
    /// </summary>
    public CsrSignatureAlgorithm SignatureAlgorithm { get; set; }

    /// <summary>
    /// Signature algorithm display label.
    /// </summary>
    public string SignatureAlgorithmDisplay => SignatureAlgorithm switch
    {
        CsrSignatureAlgorithm.SHA256WithRSA => "SHA256-RSA",
        CsrSignatureAlgorithm.SHA384WithRSA => "SHA384-RSA",
        CsrSignatureAlgorithm.SHA512WithRSA => "SHA512-RSA",
        CsrSignatureAlgorithm.SHA256WithECDSA => "SHA256-ECDSA",
        CsrSignatureAlgorithm.SHA384WithECDSA => "SHA384-ECDSA",
        CsrSignatureAlgorithm.SHA512WithECDSA => "SHA512-ECDSA",
        _ => SignatureAlgorithm.ToString()
    };

    /// <summary>
    /// Common Name (CN) from the subject.
    /// </summary>
    public string CommonName { get; set; } = string.Empty;

    /// <summary>
    /// Organization (O) from the subject.
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// Organization display (with fallback to dash).
    /// </summary>
    public string OrganizationDisplay => string.IsNullOrEmpty(Organization) ? "-" : Organization;

    /// <summary>
    /// Organizational Unit (OU) from the subject.
    /// </summary>
    public string? OrganizationalUnit { get; set; }

    /// <summary>
    /// Locality (L) from the subject.
    /// </summary>
    public string? Locality { get; set; }

    /// <summary>
    /// State/Province (ST) from the subject.
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Country (C) from the subject.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Full subject DN string.
    /// </summary>
    public string SubjectDN { get; set; } = string.Empty;

    /// <summary>
    /// Number of SANs in the CSR.
    /// </summary>
    public int SanCount { get; set; }

    /// <summary>
    /// Whether this CSR has SANs.
    /// </summary>
    public bool HasSans => SanCount > 0;

    /// <summary>
    /// List of SAN entries as formatted strings (e.g., "DNS: example.com").
    /// </summary>
    public List<string> SanList { get; set; } = new();

    /// <summary>
    /// UTC timestamp when the CSR was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Formatted date for display.
    /// </summary>
    public string CreatedDateDisplay => CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy");

    /// <summary>
    /// Whether this CSR has a private key.
    /// </summary>
    public bool HasPrivateKey { get; set; }

    /// <summary>
    /// Whether there are additional subject details to show.
    /// </summary>
    public bool HasSubjectDetails =>
        !string.IsNullOrEmpty(OrganizationalUnit) ||
        !string.IsNullOrEmpty(Locality) ||
        !string.IsNullOrEmpty(State) ||
        !string.IsNullOrEmpty(Country);

    /// <summary>
    /// Origin of the CSR (Generated or Imported).
    /// </summary>
    public CsrOrigin Origin { get; set; } = CsrOrigin.Generated;

    /// <summary>
    /// Whether this CSR was imported from an external file.
    /// </summary>
    public bool IsImported => Origin == CsrOrigin.Imported;

    /// <summary>
    /// Whether this item's copy button was recently clicked.
    /// </summary>
    [ObservableProperty]
    private bool _isCopied;
}
