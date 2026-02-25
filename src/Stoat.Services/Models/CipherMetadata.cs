using Stoat.Services.Enums;

namespace Stoat.Services.Models;

/// <summary>
/// Provides metadata about cipher algorithms: block sizes, supported key sizes, and compatible modes.
/// </summary>
public static class CipherMetadata
{
    /// <summary>
    /// Gets the block size in bits for a given cipher.
    /// </summary>
    public static int GetBlockSizeBits(SymmetricCipher cipher) => cipher switch
    {
        SymmetricCipher.AES => 128,
        SymmetricCipher.Twofish => 128,
        SymmetricCipher.Serpent => 128,
        SymmetricCipher.Camellia => 128,
        SymmetricCipher.ARIA => 128,
        SymmetricCipher.SM4 => 128,
        SymmetricCipher.SEED => 128,
        SymmetricCipher.CAST6 => 128,
        SymmetricCipher.Blowfish => 64,
        SymmetricCipher.CAST5 => 64,
        SymmetricCipher.IDEA => 64,
        SymmetricCipher.TripleDES => 64,
        SymmetricCipher.ChaCha20 => 0, // stream cipher, no block
        _ => throw new ArgumentException($"Unknown cipher: {cipher}")
    };

    /// <summary>
    /// Gets the supported key sizes in bits for a given cipher.
    /// </summary>
    public static int[] GetSupportedKeySizes(SymmetricCipher cipher) => cipher switch
    {
        SymmetricCipher.AES => [128, 192, 256],
        SymmetricCipher.Twofish => [128, 192, 256],
        SymmetricCipher.Serpent => [128, 192, 256],
        SymmetricCipher.Camellia => [128, 192, 256],
        SymmetricCipher.ARIA => [128, 192, 256],
        SymmetricCipher.SM4 => [128],
        SymmetricCipher.SEED => [128],
        SymmetricCipher.CAST6 => [128, 160, 192, 224, 256],
        SymmetricCipher.Blowfish => [128, 192, 256, 448],
        SymmetricCipher.CAST5 => [128],
        SymmetricCipher.IDEA => [128],
        SymmetricCipher.TripleDES => [192],
        SymmetricCipher.ChaCha20 => [256],
        _ => throw new ArgumentException($"Unknown cipher: {cipher}")
    };

    /// <summary>
    /// Gets the supported block modes for a given cipher.
    /// </summary>
    public static CipherBlockMode[] GetSupportedModes(SymmetricCipher cipher)
    {
        if (cipher == SymmetricCipher.ChaCha20)
        {
            // ChaCha20-Poly1305 is always AEAD, no separate mode selection
            return [];
        }

        var blockSize = GetBlockSizeBits(cipher);

        if (blockSize == 64)
        {
            // 64-bit block ciphers: no AEAD modes (GCM requires 128-bit blocks)
            return [CipherBlockMode.CBC, CipherBlockMode.CFB, CipherBlockMode.CTR, CipherBlockMode.OFB, CipherBlockMode.ECB];
        }

        // 128-bit block ciphers: all modes
        return [CipherBlockMode.CBC, CipherBlockMode.CFB, CipherBlockMode.CTR, CipherBlockMode.OFB, CipherBlockMode.ECB,
                CipherBlockMode.GCM, CipherBlockMode.CCM, CipherBlockMode.EAX];
    }

    /// <summary>
    /// Gets the default key size in bits for a given cipher.
    /// </summary>
    public static int GetDefaultKeySizeBits(SymmetricCipher cipher)
    {
        var sizes = GetSupportedKeySizes(cipher);
        return sizes[^1]; // largest key size
    }

    /// <summary>
    /// Returns true if the cipher uses 64-bit blocks (vulnerable to birthday attack on large data).
    /// </summary>
    public static bool Is64BitBlockCipher(SymmetricCipher cipher) =>
        GetBlockSizeBits(cipher) == 64;

    /// <summary>
    /// Gets the IV/nonce size in bytes for the given cipher and mode.
    /// </summary>
    public static int GetIvSizeBytes(SymmetricCipher cipher, CipherBlockMode mode)
    {
        if (cipher == SymmetricCipher.ChaCha20)
            return 12; // ChaCha20-Poly1305 nonce

        return mode switch
        {
            CipherBlockMode.GCM => 12,
            CipherBlockMode.CCM => 12,
            CipherBlockMode.EAX => 16,
            _ => GetBlockSizeBits(cipher) / 8
        };
    }
}
