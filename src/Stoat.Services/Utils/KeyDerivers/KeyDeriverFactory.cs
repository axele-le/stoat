using Stoat.Services.Enums;
using Stoat.Services.Interfaces;
using Stoat.Services.Models;

namespace Stoat.Services.Utils.KeyDerivers;

/// <summary>
/// Factory that creates the appropriate IKeyDeriver for given settings.
/// </summary>
public static class KeyDeriverFactory
{
    /// <summary>
    /// Creates an IKeyDeriver based on the encryption settings.
    /// </summary>
    public static IKeyDeriver Create(EncryptionSettings settings)
    {
        return settings.Kdf switch
        {
            KeyDerivationFunction.PBKDF2 =>
                new Pbkdf2KeyDeriver(settings.KdfHash, settings.KdfIterations),

            KeyDerivationFunction.Argon2id =>
                new Argon2KeyDeriver(settings.Argon2MemoryKB, settings.Argon2Parallelism, settings.Argon2Iterations),

            KeyDerivationFunction.Scrypt =>
                new ScryptKeyDeriver(settings.ScryptN, settings.ScryptR, settings.ScryptP),

            KeyDerivationFunction.HKDF =>
                new HkdfKeyDeriver(settings.KdfHash),

            _ => throw new ArgumentException($"Unsupported KDF: {settings.Kdf}")
        };
    }
}
