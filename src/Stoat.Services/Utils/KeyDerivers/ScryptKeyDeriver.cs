using Stoat.Services.Interfaces;
using Org.BouncyCastle.Crypto.Generators;

namespace Stoat.Services.Utils.KeyDerivers;

/// <summary>
/// Scrypt key deriver using BouncyCastle.
/// </summary>
public class ScryptKeyDeriver : IKeyDeriver
{
    private readonly int _n;
    private readonly int _r;
    private readonly int _p;

    public ScryptKeyDeriver(int n = 16384, int r = 8, int p = 1)
    {
        _n = n;
        _r = r;
        _p = p;
    }

    public (byte[] Key, byte[] Iv) DeriveKey(byte[] password, byte[] salt, int keySizeBytes, int ivSizeBytes)
    {
        var totalBytes = keySizeBytes + ivSizeBytes;

        var output = SCrypt.Generate(password, salt, _n, _r, _p, totalBytes);

        var key = new byte[keySizeBytes];
        var iv = new byte[ivSizeBytes];
        Array.Copy(output, 0, key, 0, keySizeBytes);
        Array.Copy(output, keySizeBytes, iv, 0, ivSizeBytes);

        return (key, iv);
    }
}
