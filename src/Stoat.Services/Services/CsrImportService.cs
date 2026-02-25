using System.Text;
using Stoat.Services.Enums;
using Stoat.Services.Interfaces;
using Stoat.Services.Models;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace Stoat.Services.Services;

/// <summary>
/// Service for importing CSR and private key files from external sources (PEM format).
/// </summary>
public class CsrImportService : ICsrImportService
{
    private const long MaxFileSizeBytes = 1 * 1024 * 1024; // 1 MB limit

    /// <inheritdoc />
    public async Task<CsrImportResult> ParseCsrFileAsync(string filePath)
    {
        try
        {
            var validationError = ValidateFilePath(filePath, new[] { ".csr", ".pem" });
            if (validationError != null)
                return CsrImportResult.Failure(validationError);

            var content = await File.ReadAllTextAsync(filePath);

            if (!content.Contains("-----BEGIN CERTIFICATE REQUEST-----"))
                return CsrImportResult.Failure("Il file non contiene un CSR valido in formato PEM. Atteso: BEGIN CERTIFICATE REQUEST.");

            // Parse CSR
            var csr = ParseCsrFromPem(content);
            if (csr == null)
                return CsrImportResult.Failure("Impossibile leggere il CSR. Il formato PEM potrebbe essere corrotto.");

            // Validate signature
            if (!csr.Verify())
                return CsrImportResult.Failure("La firma del CSR non è valida.");

            // Detect algorithms
            var keyAlgorithm = DetectKeyAlgorithmFromCsr(csr);
            if (keyAlgorithm == null)
                return CsrImportResult.Failure("Algoritmo della chiave non supportato. Supportati: RSA (2048-4096), ECDSA (P-256, P-384, P-521).");

            var signatureAlgorithm = DetectSignatureAlgorithmFromCsr(csr);

            // Parse subject data
            var parsedData = ParseSubjectFromCsr(csr);

            // Extract public key
            var publicKeyPem = ExtractPublicKeyPem(csr);

            // Extract SANs
            var (sanCount, sanList) = ExtractSansFromCsr(csr);

            // Build subject DN string
            var subjectDN = csr.GetCertificationRequestInfo().Subject.ToString();

            return new CsrImportResult
            {
                Success = true,
                CsrPem = content.Trim(),
                PublicKeyPem = publicKeyPem,
                ParsedData = parsedData,
                DetectedKeyAlgorithm = keyAlgorithm,
                DetectedSignatureAlgorithm = signatureAlgorithm,
                AlgorithmDisplay = GetAlgorithmDisplayString(keyAlgorithm.Value),
                SubjectDN = subjectDN,
                SanCount = sanCount,
                SanList = sanList,
                SourceFileName = Path.GetFileName(filePath)
            };
        }
        catch (Exception ex)
        {
            return CsrImportResult.Failure($"Errore durante la lettura del file CSR: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CsrImportResult> ParsePrivateKeyFileAsync(string filePath, string? password = null)
    {
        try
        {
            var validationError = ValidateFilePath(filePath, new[] { ".key", ".pem" });
            if (validationError != null)
                return CsrImportResult.Failure(validationError);

            var content = await File.ReadAllTextAsync(filePath);

            if (!content.Contains("-----BEGIN") || !content.Contains("PRIVATE KEY-----"))
                return CsrImportResult.Failure("Il file non contiene una chiave privata valida in formato PEM.");

            // Parse private key
            AsymmetricCipherKeyPair keyPair;
            try
            {
                keyPair = ParsePrivateKeyPem(content, password);
            }
            catch (PasswordException)
            {
                return CsrImportResult.Failure("La chiave privata è protetta da password. Inserire la password corretta.");
            }
            catch (Exception ex) when (ex.Message.Contains("password") || ex.Message.Contains("decrypt"))
            {
                return CsrImportResult.Failure("Password errata o formato della chiave non supportato.");
            }

            // Detect algorithm
            var keyAlgorithm = DetectKeyAlgorithmFromKeyPair(keyPair);
            if (keyAlgorithm == null)
                return CsrImportResult.Failure("Algoritmo della chiave non supportato. Supportati: RSA (2048-4096), ECDSA (P-256, P-384, P-521).");

            // Export in standard PEM format
            var privateKeyPem = ToPem(keyPair.Private);
            var publicKeyPem = ToPem(keyPair.Public);

            return new CsrImportResult
            {
                Success = true,
                PrivateKeyPem = privateKeyPem,
                PublicKeyPem = publicKeyPem,
                DetectedKeyAlgorithm = keyAlgorithm,
                AlgorithmDisplay = GetAlgorithmDisplayString(keyAlgorithm.Value),
                SourceFileName = Path.GetFileName(filePath)
            };
        }
        catch (Exception ex)
        {
            return CsrImportResult.Failure($"Errore durante la lettura della chiave privata: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public bool ValidateKeyPairMatch(string csrPem, string privateKeyPem)
    {
        try
        {
            var csr = ParseCsrFromPem(csrPem);
            if (csr == null) return false;

            var keyPair = ParsePrivateKeyPem(privateKeyPem, null);

            // Compare public keys by their encoded form
            var csrPublicKey = csr.GetPublicKey();
            var keyPublicKey = keyPair.Public;

            var csrEncoded = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(csrPublicKey).GetEncoded();
            var keyEncoded = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyPublicKey).GetEncoded();

            return csrEncoded.SequenceEqual(keyEncoded);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public CsrKeyAlgorithm? DetectKeyAlgorithm(string pem)
    {
        try
        {
            if (pem.Contains("CERTIFICATE REQUEST"))
            {
                var csr = ParseCsrFromPem(pem);
                return csr != null ? DetectKeyAlgorithmFromCsr(csr) : null;
            }

            if (pem.Contains("PRIVATE KEY"))
            {
                var keyPair = ParsePrivateKeyPem(pem, null);
                return DetectKeyAlgorithmFromKeyPair(keyPair);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public CsrSignatureAlgorithm? DetectSignatureAlgorithm(string csrPem)
    {
        try
        {
            var csr = ParseCsrFromPem(csrPem);
            return csr != null ? DetectSignatureAlgorithmFromCsr(csr) : null;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public string? ExtractPublicKeyFromCsr(string csrPem)
    {
        try
        {
            var csr = ParseCsrFromPem(csrPem);
            return csr != null ? ExtractPublicKeyPem(csr) : null;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public string? DerivePublicKeyFromPrivateKey(string privateKeyPem)
    {
        try
        {
            var keyPair = ParsePrivateKeyPem(privateKeyPem, null);
            return ToPem(keyPair.Public);
        }
        catch
        {
            return null;
        }
    }

    #region Private helpers

    private static string? ValidateFilePath(string filePath, string[] allowedExtensions)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return "Percorso file non specificato.";

        if (!File.Exists(filePath))
            return "Il file specificato non esiste.";

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return $"Estensione file non supportata: {extension}. Supportate: {string.Join(", ", allowedExtensions)}";

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length > MaxFileSizeBytes)
            return $"Il file è troppo grande ({fileInfo.Length / 1024} KB). Dimensione massima: {MaxFileSizeBytes / 1024} KB.";

        return null;
    }

    private static Pkcs10CertificationRequest? ParseCsrFromPem(string pem)
    {
        using var reader = new StringReader(pem);
        var pemReader = new PemReader(reader);
        return pemReader.ReadObject() as Pkcs10CertificationRequest;
    }

    private static AsymmetricCipherKeyPair ParsePrivateKeyPem(string pem, string? password)
    {
        using var reader = new StringReader(pem);
        var pemReader = password != null
            ? new PemReader(reader, new PasswordFinder(password))
            : new PemReader(reader);

        var obj = pemReader.ReadObject();

        return obj switch
        {
            AsymmetricCipherKeyPair keyPair => keyPair,
            RsaPrivateCrtKeyParameters rsaPrivate => new AsymmetricCipherKeyPair(
                new RsaKeyParameters(false, rsaPrivate.Modulus, rsaPrivate.PublicExponent),
                rsaPrivate),
            ECPrivateKeyParameters ecPrivate => new AsymmetricCipherKeyPair(
                GetEcPublicKeyFromPrivate(ecPrivate),
                ecPrivate),
            _ => throw new ArgumentException($"Formato chiave privata non supportato: {obj?.GetType().Name ?? "null"}")
        };
    }

    private static ECPublicKeyParameters GetEcPublicKeyFromPrivate(ECPrivateKeyParameters privateKey)
    {
        var q = privateKey.Parameters.G.Multiply(privateKey.D);
        return new ECPublicKeyParameters(q, privateKey.Parameters);
    }

    private static CsrKeyAlgorithm? DetectKeyAlgorithmFromCsr(Pkcs10CertificationRequest csr)
    {
        var publicKey = csr.GetPublicKey();
        return DetectKeyAlgorithmFromPublicKey(publicKey);
    }

    private static CsrKeyAlgorithm? DetectKeyAlgorithmFromKeyPair(AsymmetricCipherKeyPair keyPair)
    {
        return DetectKeyAlgorithmFromPublicKey(keyPair.Public);
    }

    private static CsrKeyAlgorithm? DetectKeyAlgorithmFromPublicKey(AsymmetricKeyParameter publicKey)
    {
        return publicKey switch
        {
            RsaKeyParameters rsa => rsa.Modulus.BitLength switch
            {
                2048 => CsrKeyAlgorithm.RSA_2048,
                3072 => CsrKeyAlgorithm.RSA_3072,
                4096 => CsrKeyAlgorithm.RSA_4096,
                _ => null // Unsupported RSA key size
            },
            ECPublicKeyParameters ec => GetEcCurveName(ec) switch
            {
                "secp256r1" or "P-256" or "prime256v1" => CsrKeyAlgorithm.ECDSA_P256,
                "secp384r1" or "P-384" => CsrKeyAlgorithm.ECDSA_P384,
                "secp521r1" or "P-521" => CsrKeyAlgorithm.ECDSA_P521,
                _ => null // Unsupported curve
            },
            _ => null
        };
    }

    private static string? GetEcCurveName(ECPublicKeyParameters ecKey)
    {
        // Try to match the curve by comparing domain parameters
        var knownCurves = new[] { "secp256r1", "secp384r1", "secp521r1" };

        foreach (var name in knownCurves)
        {
            var knownParams = ECNamedCurveTable.GetByName(name);
            if (knownParams != null && ecKey.Parameters.Curve.Equals(knownParams.Curve))
                return name;
        }

        return null;
    }

    private static CsrSignatureAlgorithm? DetectSignatureAlgorithmFromCsr(Pkcs10CertificationRequest csr)
    {
        var sigAlgOid = csr.SignatureAlgorithm.Algorithm.Id;

        return sigAlgOid switch
        {
            "1.2.840.113549.1.1.11" => CsrSignatureAlgorithm.SHA256WithRSA,
            "1.2.840.113549.1.1.12" => CsrSignatureAlgorithm.SHA384WithRSA,
            "1.2.840.113549.1.1.13" => CsrSignatureAlgorithm.SHA512WithRSA,
            "1.2.840.113549.1.1.5" => CsrSignatureAlgorithm.SHA1WithRSA,
            "1.2.840.10045.4.3.2" => CsrSignatureAlgorithm.SHA256WithECDSA,
            "1.2.840.10045.4.3.3" => CsrSignatureAlgorithm.SHA384WithECDSA,
            "1.2.840.10045.4.3.4" => CsrSignatureAlgorithm.SHA512WithECDSA,
            "1.2.840.10045.4.1" => CsrSignatureAlgorithm.SHA1WithECDSA,
            "2.16.840.1.101.3.4.3.14" => CsrSignatureAlgorithm.SHA3_256WithRSA,
            "2.16.840.1.101.3.4.3.15" => CsrSignatureAlgorithm.SHA3_384WithRSA,
            "2.16.840.1.101.3.4.3.16" => CsrSignatureAlgorithm.SHA3_512WithRSA,
            "2.16.840.1.101.3.4.3.10" => CsrSignatureAlgorithm.SHA3_256WithECDSA,
            "2.16.840.1.101.3.4.3.11" => CsrSignatureAlgorithm.SHA3_384WithECDSA,
            "2.16.840.1.101.3.4.3.12" => CsrSignatureAlgorithm.SHA3_512WithECDSA,
            _ => null
        };
    }

    private static CsrData ParseSubjectFromCsr(Pkcs10CertificationRequest csr)
    {
        var subject = csr.GetCertificationRequestInfo().Subject;
        var data = new CsrData();

        foreach (var oid in subject.GetOidList())
        {
            var value = subject.GetValueList(oid).Cast<string>().FirstOrDefault();
            if (value == null) continue;

            if (oid.Equals(X509Name.CN))
                data.CommonName = value;
            else if (oid.Equals(X509Name.O))
                data.Organization = value;
            else if (oid.Equals(X509Name.OU))
                data.OrganizationalUnit = value;
            else if (oid.Equals(X509Name.C))
                data.Country = value;
            else if (oid.Equals(X509Name.ST))
                data.State = value;
            else if (oid.Equals(X509Name.L))
                data.Locality = value;
            else if (oid.Equals(X509Name.EmailAddress))
                data.EmailAddress = value;
        }

        return data;
    }

    private static string? ExtractPublicKeyPem(Pkcs10CertificationRequest csr)
    {
        try
        {
            var publicKey = csr.GetPublicKey();
            return ToPem(publicKey);
        }
        catch
        {
            return null;
        }
    }

    private static (int Count, List<string> List) ExtractSansFromCsr(Pkcs10CertificationRequest csr)
    {
        var sanList = new List<string>();

        try
        {
            var info = csr.GetCertificationRequestInfo();
            var attributes = info.Attributes;

            if (attributes == null)
                return (0, sanList);

            for (int i = 0; i < attributes.Count; i++)
            {
                var attr = Org.BouncyCastle.Asn1.Pkcs.AttributePkcs.GetInstance(attributes[i]);
                if (!attr.AttrType.Equals(Org.BouncyCastle.Asn1.Pkcs.PkcsObjectIdentifiers.Pkcs9AtExtensionRequest))
                    continue;

                var extensions = X509Extensions.GetInstance(attr.AttrValues[0]);
                var sanExtension = extensions.GetExtension(X509Extensions.SubjectAlternativeName);
                if (sanExtension == null) continue;

                var generalNames = GeneralNames.GetInstance(sanExtension.GetParsedValue());
                foreach (var gn in generalNames.GetNames())
                {
                    var prefix = gn.TagNo switch
                    {
                        GeneralName.DnsName => "DNS",
                        GeneralName.IPAddress => "IP",
                        GeneralName.Rfc822Name => "Email",
                        GeneralName.UniformResourceIdentifier => "URI",
                        _ => $"Tag{gn.TagNo}"
                    };
                    sanList.Add($"{prefix}: {gn.Name}");
                }
            }
        }
        catch
        {
            // SAN extraction is best-effort
        }

        return (sanList.Count, sanList);
    }

    private static string ToPem(object obj)
    {
        using var writer = new StringWriter();
        var pemWriter = new PemWriter(writer);
        pemWriter.WriteObject(obj);
        return writer.ToString();
    }

    private static string GetAlgorithmDisplayString(CsrKeyAlgorithm algorithm)
    {
        return algorithm switch
        {
            CsrKeyAlgorithm.RSA_2048 => "RSA-2048",
            CsrKeyAlgorithm.RSA_3072 => "RSA-3072",
            CsrKeyAlgorithm.RSA_4096 => "RSA-4096",
            CsrKeyAlgorithm.ECDSA_P256 => "ECDSA-P256",
            CsrKeyAlgorithm.ECDSA_P384 => "ECDSA-P384",
            CsrKeyAlgorithm.ECDSA_P521 => "ECDSA-P521",
            _ => algorithm.ToString()
        };
    }

    #endregion

    /// <summary>
    /// IPasswordFinder implementation for BouncyCastle PEM reader.
    /// </summary>
    private class PasswordFinder : IPasswordFinder
    {
        private readonly string _password;
        public PasswordFinder(string password) => _password = password;
        public char[] GetPassword() => _password.ToCharArray();
    }
}
