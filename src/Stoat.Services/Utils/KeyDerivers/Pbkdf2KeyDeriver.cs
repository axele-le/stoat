using System.Security.Cryptography;
using Stoat.Services.Enums;
using Stoat.Services.Interfaces;

namespace Stoat.Services.Utils.KeyDerivers;

/// <summary>
/// PBKDF2 key deriver using .NET's Rfc2898DeriveBytes.
/// </summary>
public class Pbkdf2KeyDeriver : IKeyDeriver
{
    private readonly KdfHashAlgorithm _hashAlgorithm;
    private readonly int _iterations;

    public Pbkdf2KeyDeriver(KdfHashAlgorithm hashAlgorithm, int iterations)
    {
        _hashAlgorithm = hashAlgorithm;
        _iterations = iterations;
    }

    public (byte[] Key, byte[] Iv) DeriveKey(byte[] password, byte[] salt, int keySizeBytes, int ivSizeBytes)
    {
        var hashName = _hashAlgorithm switch
        {
            KdfHashAlgorithm.SHA1 => HashAlgorithmName.SHA1,
            KdfHashAlgorithm.SHA256 => HashAlgorithmName.SHA256,
            KdfHashAlgorithm.SHA384 => HashAlgorithmName.SHA384,
            KdfHashAlgorithm.SHA512 => HashAlgorithmName.SHA512,
            // SHA3 not supported by Rfc2898DeriveBytes, fall back to BouncyCastle
            KdfHashAlgorithm.SHA3_256 or KdfHashAlgorithm.SHA3_384 or KdfHashAlgorithm.SHA3_512
                => throw new NotSupportedException($"SHA3 variants are not supported with PBKDF2. Use Argon2id or Scrypt instead."),
            _ => HashAlgorithmName.SHA256
        };

        using var kdf = new Rfc2898DeriveBytes(password, salt, _iterations, hashName);
        var key = kdf.GetBytes(keySizeBytes);
        var iv = kdf.GetBytes(ivSizeBytes);
        return (key, iv);
    }
}
