using Stoat.Services.Enums;
using Stoat.Services.Interfaces;
using Stoat.Services.Utils.Engines;

namespace Stoat.Services.Utils;

/// <summary>
/// Factory that creates the appropriate ICipherEngine for a given cipher and mode combination.
/// </summary>
public static class CipherEngineFactory
{
    /// <summary>
    /// Creates an ICipherEngine for the given cipher and mode.
    /// </summary>
    public static ICipherEngine Create(SymmetricCipher cipher, CipherBlockMode mode)
    {
        // ChaCha20 is always AEAD (ChaCha20-Poly1305)
        if (cipher == SymmetricCipher.ChaCha20)
            return new ChaCha20Poly1305Engine();

        // AEAD modes
        if (mode is CipherBlockMode.GCM or CipherBlockMode.CCM or CipherBlockMode.EAX)
            return new AeadCipherEngine(cipher, mode);

        // Standard block cipher modes
        return new BlockCipherEngine(cipher, mode);
    }
}
