using System.Security.Cryptography;
using System.Text;
using Stoat.Services.Interfaces;
using Stoat.Services.Models;
using Stoat.Services.Utils;

namespace Stoat.Services.Services;

/// <summary>
/// Service for generating and validating RSA PEM key pairs.
/// </summary>
public class PemService : IPemService
{
    /// <inheritdoc />
    public PEMs GenerateKeyPair(int keySize = 4096)
    {
        if (keySize < 2048)
            throw new ArgumentOutOfRangeException(nameof(keySize), "Key size must be at least 2048 bits.");

        return PEMGenerator.GeneratePEMKeys(keySize);
    }

    /// <inheritdoc />
    public bool ValidatePublicKey(string pemPublicKey)
    {
        if (string.IsNullOrWhiteSpace(pemPublicKey))
            return false;

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(pemPublicKey);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool ValidatePrivateKey(string pemPrivateKey)
    {
        if (string.IsNullOrWhiteSpace(pemPrivateKey))
            return false;

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(pemPrivateKey);

            // Verify it has private key material by trying to export
            _ = rsa.ExportPkcs8PrivateKey();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public string? ExtractPublicKeyFromPrivate(string pemPrivateKey)
    {
        if (!ValidatePrivateKey(pemPrivateKey))
            return null;

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(pemPrivateKey);

            var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
            return ConvertToPem(publicKeyBytes, "PUBLIC KEY");
        }
        catch
        {
            return null;
        }
    }

    private static string ConvertToPem(byte[] keyBytes, string keyType)
    {
        var base64Key = Convert.ToBase64String(keyBytes);
        var builder = new StringBuilder();

        builder.AppendLine($"-----BEGIN {keyType}-----");

        for (int i = 0; i < base64Key.Length; i += 64)
        {
            int length = Math.Min(64, base64Key.Length - i);
            builder.AppendLine(base64Key.Substring(i, length));
        }

        builder.AppendLine($"-----END {keyType}-----");
        return builder.ToString();
    }
}
