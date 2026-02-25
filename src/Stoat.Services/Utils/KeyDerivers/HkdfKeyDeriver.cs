using System.Security.Cryptography;
using Stoat.Services.Enums;
using Stoat.Services.Interfaces;

namespace Stoat.Services.Utils.KeyDerivers;

/// <summary>
/// HKDF key deriver using .NET native HKDF.
/// </summary>
public class HkdfKeyDeriver : IKeyDeriver
{
    private readonly KdfHashAlgorithm _hashAlgorithm;

    public HkdfKeyDeriver(KdfHashAlgorithm hashAlgorithm = KdfHashAlgorithm.SHA256)
    {
        _hashAlgorithm = hashAlgorithm;
    }

    public (byte[] Key, byte[] Iv) DeriveKey(byte[] password, byte[] salt, int keySizeBytes, int ivSizeBytes)
    {
        var hashName = _hashAlgorithm switch
        {
            KdfHashAlgorithm.SHA1 => HashAlgorithmName.SHA1,
            KdfHashAlgorithm.SHA256 => HashAlgorithmName.SHA256,
            KdfHashAlgorithm.SHA384 => HashAlgorithmName.SHA384,
            KdfHashAlgorithm.SHA512 => HashAlgorithmName.SHA512,
            _ => HashAlgorithmName.SHA256
        };

        // HKDF: Extract then Expand
        var prk = HKDF.Extract(hashName, password, salt);
        var key = HKDF.Expand(hashName, prk, keySizeBytes, "ccg-key"u8.ToArray());
        var iv = HKDF.Expand(hashName, prk, ivSizeBytes, "ccg-iv"u8.ToArray());

        return (key, iv);
    }
}
