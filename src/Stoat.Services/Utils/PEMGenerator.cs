using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Stoat.Services.Models;

namespace Stoat.Services.Utils;

public static class PEMGenerator
{
    public static PEMs GeneratePEMKeys(int keySize = 4096)
    {
        var pemKeys = new PEMs();

        using (var rsa = RSA.Create(keySize))
        {
            var privateKeyBytes = rsa.ExportPkcs8PrivateKey();
            string pemPrivateKey = ConvertToPEM(privateKeyBytes, "PRIVATE KEY");

            pemKeys.PrivateKey = pemPrivateKey;

            var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
            string pemPublicKey = ConvertToPEM(publicKeyBytes, "PUBLIC KEY");

            pemKeys.PublicKey = pemPublicKey;
        }

        return pemKeys;
    }

    private static string ConvertToPEM(byte[] keyBytes, string keyType)
    {
        string base64Key = Convert.ToBase64String(keyBytes);
        StringBuilder pemBuilder = new StringBuilder();

        pemBuilder.AppendLine($"-----BEGIN {keyType}-----");

        int lineLength = 64;
        for (int i = 0; i < base64Key.Length; i += lineLength)
        {
            int currentLineLength = Math.Min(lineLength, base64Key.Length - i);
            pemBuilder.AppendLine(base64Key.Substring(i, currentLineLength));
        }

        pemBuilder.AppendLine($"-----END {keyType}-----");
        return pemBuilder.ToString();
    }
}
