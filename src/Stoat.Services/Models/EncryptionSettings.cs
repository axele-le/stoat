using Stoat.Services.Enums;

namespace Stoat.Services.Models;

/// <summary>
/// Configuration settings for encryption operations supporting multiple ciphers and KDFs.
/// </summary>
public class EncryptionSettings
{
    /// <summary>
    /// The symmetric cipher algorithm to use.
    /// </summary>
    public SymmetricCipher Cipher { get; set; } = SymmetricCipher.AES;

    /// <summary>
    /// Key size in bits.
    /// </summary>
    public int KeySizeBits { get; set; } = 256;

    /// <summary>
    /// Cipher block mode.
    /// </summary>
    public CipherBlockMode Mode { get; set; } = CipherBlockMode.CBC;

    /// <summary>
    /// Key derivation function to use.
    /// </summary>
    public KeyDerivationFunction Kdf { get; set; } = KeyDerivationFunction.PBKDF2;

    /// <summary>
    /// Hash algorithm for the KDF.
    /// </summary>
    public KdfHashAlgorithm KdfHash { get; set; } = KdfHashAlgorithm.SHA256;

    /// <summary>
    /// Number of iterations for PBKDF2.
    /// </summary>
    public int KdfIterations { get; set; } = 100000;

    /// <summary>
    /// Argon2 memory cost in KB.
    /// </summary>
    public int Argon2MemoryKB { get; set; } = 65536;

    /// <summary>
    /// Argon2 parallelism (number of threads).
    /// </summary>
    public int Argon2Parallelism { get; set; } = 4;

    /// <summary>
    /// Argon2 iterations (time cost).
    /// </summary>
    public int Argon2Iterations { get; set; } = 3;

    /// <summary>
    /// Scrypt CPU/memory cost parameter (N). Must be a power of 2.
    /// </summary>
    public int ScryptN { get; set; } = 16384;

    /// <summary>
    /// Scrypt block size parameter (r).
    /// </summary>
    public int ScryptR { get; set; } = 8;

    /// <summary>
    /// Scrypt parallelism parameter (p).
    /// </summary>
    public int ScryptP { get; set; } = 1;

    /// <summary>
    /// Creates a new instance with default values.
    /// </summary>
    public static EncryptionSettings Default => new();

    /// <summary>
    /// Creates a copy of the current settings.
    /// </summary>
    public EncryptionSettings Clone() => new()
    {
        Cipher = Cipher,
        KeySizeBits = KeySizeBits,
        Mode = Mode,
        Kdf = Kdf,
        KdfHash = KdfHash,
        KdfIterations = KdfIterations,
        Argon2MemoryKB = Argon2MemoryKB,
        Argon2Parallelism = Argon2Parallelism,
        Argon2Iterations = Argon2Iterations,
        ScryptN = ScryptN,
        ScryptR = ScryptR,
        ScryptP = ScryptP
    };

    /// <summary>
    /// Returns true if all settings match.
    /// </summary>
    public bool SettingsEqual(EncryptionSettings other) =>
        Cipher == other.Cipher &&
        KeySizeBits == other.KeySizeBits &&
        Mode == other.Mode &&
        Kdf == other.Kdf &&
        KdfHash == other.KdfHash &&
        KdfIterations == other.KdfIterations &&
        Argon2MemoryKB == other.Argon2MemoryKB &&
        Argon2Parallelism == other.Argon2Parallelism &&
        Argon2Iterations == other.Argon2Iterations &&
        ScryptN == other.ScryptN &&
        ScryptR == other.ScryptR &&
        ScryptP == other.ScryptP;

    /// <summary>
    /// Returns true if this configuration is AEAD (authenticated encryption).
    /// </summary>
    public bool IsAead => Mode is CipherBlockMode.GCM or CipherBlockMode.CCM or CipherBlockMode.EAX;

    /// <summary>
    /// Returns true if this is a stream cipher (ChaCha20).
    /// </summary>
    public bool IsStreamCipher => Cipher == SymmetricCipher.ChaCha20;
}
