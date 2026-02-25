using Stoat.Services.Interfaces;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;

namespace Stoat.Services.Utils.KeyDerivers;

/// <summary>
/// Argon2id key deriver using BouncyCastle.
/// </summary>
public class Argon2KeyDeriver : IKeyDeriver
{
    private readonly int _memoryKB;
    private readonly int _parallelism;
    private readonly int _iterations;

    public Argon2KeyDeriver(int memoryKB = 65536, int parallelism = 4, int iterations = 3)
    {
        _memoryKB = memoryKB;
        _parallelism = parallelism;
        _iterations = iterations;
    }

    public (byte[] Key, byte[] Iv) DeriveKey(byte[] password, byte[] salt, int keySizeBytes, int ivSizeBytes)
    {
        var totalBytes = keySizeBytes + ivSizeBytes;

        var parameters = new Argon2Parameters.Builder(Argon2Parameters.Argon2id)
            .WithSalt(salt)
            .WithMemoryAsKB(_memoryKB)
            .WithParallelism(_parallelism)
            .WithIterations(_iterations)
            .Build();

        var generator = new Argon2BytesGenerator();
        generator.Init(parameters);

        var output = new byte[totalBytes];
        generator.GenerateBytes(password, output);

        var key = new byte[keySizeBytes];
        var iv = new byte[ivSizeBytes];
        Array.Copy(output, 0, key, 0, keySizeBytes);
        Array.Copy(output, keySizeBytes, iv, 0, ivSizeBytes);

        return (key, iv);
    }
}
