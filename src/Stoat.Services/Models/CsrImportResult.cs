using Stoat.Services.Enums;

namespace Stoat.Services.Models;

/// <summary>
/// Result of parsing/importing a CSR or private key file.
/// </summary>
public class CsrImportResult
{
    /// <summary>
    /// Whether the parsing was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if parsing failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// The CSR in PEM format (if a CSR file was parsed).
    /// </summary>
    public string? CsrPem { get; set; }

    /// <summary>
    /// The private key in PEM format (if a key file was parsed).
    /// </summary>
    public string? PrivateKeyPem { get; set; }

    /// <summary>
    /// The public key in PEM format (extracted from CSR or derived from private key).
    /// </summary>
    public string? PublicKeyPem { get; set; }

    /// <summary>
    /// Parsed CSR data (subject DN, algorithm info).
    /// </summary>
    public CsrData? ParsedData { get; set; }

    /// <summary>
    /// Detected key algorithm.
    /// </summary>
    public CsrKeyAlgorithm? DetectedKeyAlgorithm { get; set; }

    /// <summary>
    /// Detected signature algorithm (from CSR).
    /// </summary>
    public CsrSignatureAlgorithm? DetectedSignatureAlgorithm { get; set; }

    /// <summary>
    /// Algorithm display string (e.g., "RSA-2048").
    /// </summary>
    public string? AlgorithmDisplay { get; set; }

    /// <summary>
    /// Subject DN string for display.
    /// </summary>
    public string? SubjectDN { get; set; }

    /// <summary>
    /// Number of SANs found in the CSR.
    /// </summary>
    public int SanCount { get; set; }

    /// <summary>
    /// List of SANs as formatted strings.
    /// </summary>
    public List<string> SanList { get; set; } = new();

    /// <summary>
    /// Source file name.
    /// </summary>
    public string? SourceFileName { get; set; }

    public static CsrImportResult Failure(string error) => new() { Success = false, Error = error };
}
