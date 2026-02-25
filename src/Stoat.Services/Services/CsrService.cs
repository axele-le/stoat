using System.Text;
using Stoat.Services.Enums;
using Stoat.Services.Interfaces;
using Stoat.Services.Models;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace Stoat.Services.Services;

/// <summary>
/// Service for generating and validating Certificate Signing Requests (CSRs) using BouncyCastle.
/// </summary>
public class CsrService : ICsrService
{
    private static readonly SecureRandom SecureRandom = new();

    /// <inheritdoc />
    public (string CsrPem, string PrivateKeyPem, string PublicKeyPem) GenerateCsr(CsrData data)
    {
        if (string.IsNullOrWhiteSpace(data.CommonName))
            throw new ArgumentException("Common Name (CN) is required.", nameof(data));

        // Generate key pair
        var keyPair = GenerateKeyPair(data.KeyAlgorithm);

        // Build Subject DN
        var subject = BuildSubjectDN(data);

        // Build extensions
        var extensions = BuildExtensions(data);

        // Get signature algorithm
        var signatureAlgorithm = GetBouncyCastleSignatureAlgorithm(data.SignatureAlgorithm);

        // Build attributes for extensions
        DerSet? attributes = null;
        if (extensions.Count > 0)
        {
            var extensionsGenerator = new X509ExtensionsGenerator();
            foreach (var ext in extensions)
            {
                extensionsGenerator.AddExtension(ext.Oid, ext.IsCritical, ext.Value);
            }
            var generatedExtensions = extensionsGenerator.Generate();
            attributes = new DerSet(new AttributePkcs(PkcsObjectIdentifiers.Pkcs9AtExtensionRequest, new DerSet(generatedExtensions)));
        }

        // Create CSR using Pkcs10CertificationRequest constructor
        var csr = new Pkcs10CertificationRequest(
            signatureAlgorithm,
            subject,
            keyPair.Public,
            attributes,
            keyPair.Private);

        // Convert to PEM
        var csrPem = ToPem(csr);
        var privateKeyPem = ToPem(keyPair.Private);
        var publicKeyPem = ToPem(keyPair.Public);

        return (csrPem, privateKeyPem, publicKeyPem);
    }

    /// <inheritdoc />
    public (string CsrPem, string PublicKeyPem) GenerateCsrWithExistingKey(CsrData data, string privateKeyPem)
    {
        if (string.IsNullOrWhiteSpace(data.CommonName))
            throw new ArgumentException("Common Name (CN) is required.", nameof(data));

        if (string.IsNullOrWhiteSpace(privateKeyPem))
            throw new ArgumentException("Private key is required.", nameof(privateKeyPem));

        // Parse existing private key
        var keyPair = ParsePrivateKeyPem(privateKeyPem);

        // Build Subject DN
        var subject = BuildSubjectDN(data);

        // Build extensions
        var extensions = BuildExtensions(data);

        // Get signature algorithm
        var signatureAlgorithm = GetBouncyCastleSignatureAlgorithm(data.SignatureAlgorithm);

        // Build attributes for extensions
        DerSet? attributes = null;
        if (extensions.Count > 0)
        {
            var extensionsGenerator = new X509ExtensionsGenerator();
            foreach (var ext in extensions)
            {
                extensionsGenerator.AddExtension(ext.Oid, ext.IsCritical, ext.Value);
            }
            var generatedExtensions = extensionsGenerator.Generate();
            attributes = new DerSet(new AttributePkcs(PkcsObjectIdentifiers.Pkcs9AtExtensionRequest, new DerSet(generatedExtensions)));
        }

        // Create CSR using Pkcs10CertificationRequest constructor
        var csr = new Pkcs10CertificationRequest(
            signatureAlgorithm,
            subject,
            keyPair.Public,
            attributes,
            keyPair.Private);

        // Convert to PEM
        var csrPem = ToPem(csr);
        var publicKeyPem = ToPem(keyPair.Public);

        return (csrPem, publicKeyPem);
    }

    /// <inheritdoc />
    public bool ValidateCsr(string csrPem)
    {
        try
        {
            var csr = ParseCsrFromPem(csrPem);
            return csr != null && csr.Verify();
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public CsrData? ParseCsr(string csrPem)
    {
        try
        {
            var csr = ParseCsrFromPem(csrPem);
            if (csr == null) return null;

            var subject = csr.GetCertificationRequestInfo().Subject;
            var data = new CsrData();

            // Parse subject DN
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
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<CsrSignatureAlgorithm> GetCompatibleSignatureAlgorithms(CsrKeyAlgorithm keyAlgorithm)
    {
        return keyAlgorithm switch
        {
            CsrKeyAlgorithm.RSA_2048 or CsrKeyAlgorithm.RSA_3072 or CsrKeyAlgorithm.RSA_4096 =>
                new[]
                {
                    CsrSignatureAlgorithm.SHA256WithRSA,
                    CsrSignatureAlgorithm.SHA384WithRSA,
                    CsrSignatureAlgorithm.SHA512WithRSA,
                    CsrSignatureAlgorithm.SHA1WithRSA,
                    CsrSignatureAlgorithm.SHA3_256WithRSA,
                    CsrSignatureAlgorithm.SHA3_384WithRSA,
                    CsrSignatureAlgorithm.SHA3_512WithRSA
                },
            CsrKeyAlgorithm.ECDSA_P256 or CsrKeyAlgorithm.ECDSA_P384 or CsrKeyAlgorithm.ECDSA_P521 =>
                new[]
                {
                    CsrSignatureAlgorithm.SHA256WithECDSA,
                    CsrSignatureAlgorithm.SHA384WithECDSA,
                    CsrSignatureAlgorithm.SHA512WithECDSA,
                    CsrSignatureAlgorithm.SHA1WithECDSA,
                    CsrSignatureAlgorithm.SHA3_256WithECDSA,
                    CsrSignatureAlgorithm.SHA3_384WithECDSA,
                    CsrSignatureAlgorithm.SHA3_512WithECDSA
                },
            _ => Array.Empty<CsrSignatureAlgorithm>()
        };
    }

    /// <inheritdoc />
    public CsrData GetPresetDefaults(CsrPresetType preset)
    {
        return preset switch
        {
            CsrPresetType.WebServer => new CsrData
            {
                KeyAlgorithm = CsrKeyAlgorithm.RSA_2048,
                SignatureAlgorithm = CsrSignatureAlgorithm.SHA256WithRSA,
                KeyUsage = KeyUsageFlags.DigitalSignature | KeyUsageFlags.KeyEncipherment,
                ExtendedKeyUsage = new List<ExtendedKeyUsageOid>
                {
                    ExtendedKeyUsageOid.ServerAuth,
                    ExtendedKeyUsageOid.ClientAuth
                }
            },
            CsrPresetType.ClientAuth => new CsrData
            {
                KeyAlgorithm = CsrKeyAlgorithm.RSA_2048,
                SignatureAlgorithm = CsrSignatureAlgorithm.SHA256WithRSA,
                KeyUsage = KeyUsageFlags.DigitalSignature,
                ExtendedKeyUsage = new List<ExtendedKeyUsageOid>
                {
                    ExtendedKeyUsageOid.ClientAuth
                }
            },
            CsrPresetType.CodeSigning => new CsrData
            {
                KeyAlgorithm = CsrKeyAlgorithm.RSA_4096,
                SignatureAlgorithm = CsrSignatureAlgorithm.SHA256WithRSA,
                KeyUsage = KeyUsageFlags.DigitalSignature,
                ExtendedKeyUsage = new List<ExtendedKeyUsageOid>
                {
                    ExtendedKeyUsageOid.CodeSigning
                }
            },
            CsrPresetType.Email => new CsrData
            {
                KeyAlgorithm = CsrKeyAlgorithm.RSA_2048,
                SignatureAlgorithm = CsrSignatureAlgorithm.SHA256WithRSA,
                KeyUsage = KeyUsageFlags.DigitalSignature | KeyUsageFlags.KeyEncipherment,
                ExtendedKeyUsage = new List<ExtendedKeyUsageOid>
                {
                    ExtendedKeyUsageOid.EmailProtection
                }
            },
            _ => new CsrData()
        };
    }

    /// <inheritdoc />
    public string GetKeyAlgorithmDisplayName(CsrKeyAlgorithm algorithm)
    {
        return algorithm switch
        {
            CsrKeyAlgorithm.RSA_2048 => "RSA 2048-bit",
            CsrKeyAlgorithm.RSA_3072 => "RSA 3072-bit",
            CsrKeyAlgorithm.RSA_4096 => "RSA 4096-bit",
            CsrKeyAlgorithm.ECDSA_P256 => "ECDSA P-256",
            CsrKeyAlgorithm.ECDSA_P384 => "ECDSA P-384",
            CsrKeyAlgorithm.ECDSA_P521 => "ECDSA P-521",
            _ => algorithm.ToString()
        };
    }

    /// <inheritdoc />
    public string GetSignatureAlgorithmDisplayName(CsrSignatureAlgorithm algorithm)
    {
        return algorithm switch
        {
            CsrSignatureAlgorithm.SHA256WithRSA => "SHA-256 with RSA",
            CsrSignatureAlgorithm.SHA384WithRSA => "SHA-384 with RSA",
            CsrSignatureAlgorithm.SHA512WithRSA => "SHA-512 with RSA",
            CsrSignatureAlgorithm.SHA256WithECDSA => "SHA-256 with ECDSA",
            CsrSignatureAlgorithm.SHA384WithECDSA => "SHA-384 with ECDSA",
            CsrSignatureAlgorithm.SHA512WithECDSA => "SHA-512 with ECDSA",
            CsrSignatureAlgorithm.SHA1WithRSA => "SHA-1 with RSA",
            CsrSignatureAlgorithm.SHA1WithECDSA => "SHA-1 with ECDSA",
            CsrSignatureAlgorithm.SHA3_256WithRSA => "SHA3-256 with RSA",
            CsrSignatureAlgorithm.SHA3_384WithRSA => "SHA3-384 with RSA",
            CsrSignatureAlgorithm.SHA3_512WithRSA => "SHA3-512 with RSA",
            CsrSignatureAlgorithm.SHA3_256WithECDSA => "SHA3-256 with ECDSA",
            CsrSignatureAlgorithm.SHA3_384WithECDSA => "SHA3-384 with ECDSA",
            CsrSignatureAlgorithm.SHA3_512WithECDSA => "SHA3-512 with ECDSA",
            _ => algorithm.ToString()
        };
    }

    private static AsymmetricCipherKeyPair GenerateKeyPair(CsrKeyAlgorithm algorithm)
    {
        return algorithm switch
        {
            CsrKeyAlgorithm.RSA_2048 => GenerateRsaKeyPair(2048),
            CsrKeyAlgorithm.RSA_3072 => GenerateRsaKeyPair(3072),
            CsrKeyAlgorithm.RSA_4096 => GenerateRsaKeyPair(4096),
            CsrKeyAlgorithm.ECDSA_P256 => GenerateEcdsaKeyPair("secp256r1"),
            CsrKeyAlgorithm.ECDSA_P384 => GenerateEcdsaKeyPair("secp384r1"),
            CsrKeyAlgorithm.ECDSA_P521 => GenerateEcdsaKeyPair("secp521r1"),
            _ => throw new ArgumentException($"Unsupported key algorithm: {algorithm}")
        };
    }

    private static AsymmetricCipherKeyPair GenerateRsaKeyPair(int keySize)
    {
        var generator = new RsaKeyPairGenerator();
        generator.Init(new RsaKeyGenerationParameters(
            BigInteger.ValueOf(65537),
            SecureRandom,
            keySize,
            100));
        return generator.GenerateKeyPair();
    }

    private static AsymmetricCipherKeyPair GenerateEcdsaKeyPair(string curveName)
    {
        var ecParams = ECNamedCurveTable.GetByName(curveName);
        var domainParams = new ECDomainParameters(ecParams.Curve, ecParams.G, ecParams.N, ecParams.H, ecParams.GetSeed());
        var generator = new ECKeyPairGenerator();
        generator.Init(new ECKeyGenerationParameters(domainParams, SecureRandom));
        return generator.GenerateKeyPair();
    }

    private static X509Name BuildSubjectDN(CsrData data)
    {
        var oids = new List<DerObjectIdentifier>();
        var values = new List<string>();

        // Add in standard order: C, ST, L, O, OU, CN, Email
        if (!string.IsNullOrWhiteSpace(data.Country))
        {
            oids.Add(X509Name.C);
            values.Add(data.Country);
        }

        if (!string.IsNullOrWhiteSpace(data.State))
        {
            oids.Add(X509Name.ST);
            values.Add(data.State);
        }

        if (!string.IsNullOrWhiteSpace(data.Locality))
        {
            oids.Add(X509Name.L);
            values.Add(data.Locality);
        }

        if (!string.IsNullOrWhiteSpace(data.Organization))
        {
            oids.Add(X509Name.O);
            values.Add(data.Organization);
        }

        if (!string.IsNullOrWhiteSpace(data.OrganizationalUnit))
        {
            oids.Add(X509Name.OU);
            values.Add(data.OrganizationalUnit);
        }

        // Common Name is required
        oids.Add(X509Name.CN);
        values.Add(data.CommonName);

        if (!string.IsNullOrWhiteSpace(data.EmailAddress))
        {
            oids.Add(X509Name.EmailAddress);
            values.Add(data.EmailAddress);
        }

        return new X509Name(oids, values);
    }

    private static List<(DerObjectIdentifier Oid, bool IsCritical, Asn1Encodable Value)> BuildExtensions(CsrData data)
    {
        var extensions = new List<(DerObjectIdentifier Oid, bool IsCritical, Asn1Encodable Value)>();

        // Key Usage
        if (data.KeyUsage != KeyUsageFlags.None)
        {
            var keyUsage = ConvertKeyUsage(data.KeyUsage);
            extensions.Add((X509Extensions.KeyUsage, true, keyUsage));
        }

        // Extended Key Usage
        if (data.ExtendedKeyUsage.Count > 0)
        {
            var ekuOids = data.ExtendedKeyUsage.Select(ConvertExtendedKeyUsage).ToArray();
            var eku = new ExtendedKeyUsage(ekuOids);
            extensions.Add((X509Extensions.ExtendedKeyUsage, false, eku));
        }

        // Basic Constraints (for CA)
        if (data.IsCA)
        {
            var basicConstraints = new BasicConstraints(true);
            extensions.Add((X509Extensions.BasicConstraints, true, basicConstraints));
        }

        // Subject Alternative Names
        if (data.SubjectAlternativeNames.Count > 0)
        {
            var sanList = new List<GeneralName>();

            foreach (var san in data.SubjectAlternativeNames)
            {
                if (string.IsNullOrWhiteSpace(san.Value)) continue;

                GeneralName gn = san.Type switch
                {
                    SanType.DNS => new GeneralName(GeneralName.DnsName, san.Value),
                    SanType.IP => new GeneralName(GeneralName.IPAddress, san.Value),
                    SanType.Email => new GeneralName(GeneralName.Rfc822Name, san.Value),
                    SanType.URI => new GeneralName(GeneralName.UniformResourceIdentifier, san.Value),
                    _ => throw new ArgumentException($"Unsupported SAN type: {san.Type}")
                };
                sanList.Add(gn);
            }

            if (sanList.Count > 0)
            {
                var san = new GeneralNames(sanList.ToArray());
                extensions.Add((X509Extensions.SubjectAlternativeName, false, san));
            }
        }

        return extensions;
    }

    private static KeyUsage ConvertKeyUsage(KeyUsageFlags flags)
    {
        int usage = 0;

        if ((flags & KeyUsageFlags.DigitalSignature) != 0)
            usage |= KeyUsage.DigitalSignature;
        if ((flags & KeyUsageFlags.NonRepudiation) != 0)
            usage |= KeyUsage.NonRepudiation;
        if ((flags & KeyUsageFlags.KeyEncipherment) != 0)
            usage |= KeyUsage.KeyEncipherment;
        if ((flags & KeyUsageFlags.DataEncipherment) != 0)
            usage |= KeyUsage.DataEncipherment;
        if ((flags & KeyUsageFlags.KeyAgreement) != 0)
            usage |= KeyUsage.KeyAgreement;
        if ((flags & KeyUsageFlags.KeyCertSign) != 0)
            usage |= KeyUsage.KeyCertSign;
        if ((flags & KeyUsageFlags.CrlSign) != 0)
            usage |= KeyUsage.CrlSign;
        if ((flags & KeyUsageFlags.EncipherOnly) != 0)
            usage |= KeyUsage.EncipherOnly;
        if ((flags & KeyUsageFlags.DecipherOnly) != 0)
            usage |= KeyUsage.DecipherOnly;

        return new KeyUsage(usage);
    }

    private static KeyPurposeID ConvertExtendedKeyUsage(ExtendedKeyUsageOid eku)
    {
        return eku switch
        {
            ExtendedKeyUsageOid.ServerAuth => KeyPurposeID.id_kp_serverAuth,
            ExtendedKeyUsageOid.ClientAuth => KeyPurposeID.id_kp_clientAuth,
            ExtendedKeyUsageOid.CodeSigning => KeyPurposeID.id_kp_codeSigning,
            ExtendedKeyUsageOid.EmailProtection => KeyPurposeID.id_kp_emailProtection,
            ExtendedKeyUsageOid.TimeStamping => KeyPurposeID.id_kp_timeStamping,
            ExtendedKeyUsageOid.OcspSigning => KeyPurposeID.id_kp_OCSPSigning,
            _ => throw new ArgumentException($"Unsupported EKU: {eku}")
        };
    }

    private static string GetBouncyCastleSignatureAlgorithm(CsrSignatureAlgorithm algorithm)
    {
        return algorithm switch
        {
            CsrSignatureAlgorithm.SHA256WithRSA => "SHA256WITHRSA",
            CsrSignatureAlgorithm.SHA384WithRSA => "SHA384WITHRSA",
            CsrSignatureAlgorithm.SHA512WithRSA => "SHA512WITHRSA",
            CsrSignatureAlgorithm.SHA256WithECDSA => "SHA256WITHECDSA",
            CsrSignatureAlgorithm.SHA384WithECDSA => "SHA384WITHECDSA",
            CsrSignatureAlgorithm.SHA512WithECDSA => "SHA512WITHECDSA",
            CsrSignatureAlgorithm.SHA1WithRSA => "SHA1WITHRSA",
            CsrSignatureAlgorithm.SHA1WithECDSA => "SHA1WITHECDSA",
            CsrSignatureAlgorithm.SHA3_256WithRSA => "SHA3-256WITHRSA",
            CsrSignatureAlgorithm.SHA3_384WithRSA => "SHA3-384WITHRSA",
            CsrSignatureAlgorithm.SHA3_512WithRSA => "SHA3-512WITHRSA",
            CsrSignatureAlgorithm.SHA3_256WithECDSA => "SHA3-256WITHECDSA",
            CsrSignatureAlgorithm.SHA3_384WithECDSA => "SHA3-384WITHECDSA",
            CsrSignatureAlgorithm.SHA3_512WithECDSA => "SHA3-512WITHECDSA",
            _ => throw new ArgumentException($"Unsupported signature algorithm: {algorithm}")
        };
    }

    private static string ToPem(object obj)
    {
        using var writer = new StringWriter();
        var pemWriter = new PemWriter(writer);
        pemWriter.WriteObject(obj);
        return writer.ToString();
    }

    private static Pkcs10CertificationRequest? ParseCsrFromPem(string pem)
    {
        using var reader = new StringReader(pem);
        var pemReader = new PemReader(reader);
        return pemReader.ReadObject() as Pkcs10CertificationRequest;
    }

    private static AsymmetricCipherKeyPair ParsePrivateKeyPem(string pem)
    {
        using var reader = new StringReader(pem);
        var pemReader = new PemReader(reader);
        var obj = pemReader.ReadObject();

        // Handle different private key formats
        return obj switch
        {
            AsymmetricCipherKeyPair keyPair => keyPair,
            RsaPrivateCrtKeyParameters rsaPrivate => new AsymmetricCipherKeyPair(
                new RsaKeyParameters(false, rsaPrivate.Modulus, rsaPrivate.PublicExponent),
                rsaPrivate),
            ECPrivateKeyParameters ecPrivate => new AsymmetricCipherKeyPair(
                GetEcPublicKeyFromPrivate(ecPrivate),
                ecPrivate),
            _ => throw new ArgumentException($"Unsupported private key format: {obj?.GetType().Name ?? "null"}")
        };
    }

    private static ECPublicKeyParameters GetEcPublicKeyFromPrivate(ECPrivateKeyParameters privateKey)
    {
        var q = privateKey.Parameters.G.Multiply(privateKey.D);
        return new ECPublicKeyParameters(q, privateKey.Parameters);
    }
}
