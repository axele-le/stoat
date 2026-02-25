namespace Stoat.Services.Interfaces;

/// <summary>
/// Interface for symmetric cipher encryption/decryption operations.
/// </summary>
public interface ICipherEngine
{
    /// <summary>
    /// Encrypts data using standard (non-AEAD) mode.
    /// </summary>
    byte[] Encrypt(byte[] key, byte[] iv, byte[] plaintext);

    /// <summary>
    /// Decrypts data using standard (non-AEAD) mode.
    /// </summary>
    byte[] Decrypt(byte[] key, byte[] iv, byte[] ciphertext);

    /// <summary>
    /// Encrypts data using AEAD mode (GCM/CCM/EAX/ChaCha20-Poly1305).
    /// Returns ciphertext with appended authentication tag.
    /// </summary>
    byte[] EncryptAead(byte[] key, byte[] nonce, byte[] plaintext, byte[]? associatedData = null);

    /// <summary>
    /// Decrypts data using AEAD mode (GCM/CCM/EAX/ChaCha20-Poly1305).
    /// Input is ciphertext with appended authentication tag.
    /// </summary>
    byte[] DecryptAead(byte[] key, byte[] nonce, byte[] ciphertextWithTag, byte[]? associatedData = null);

    /// <summary>
    /// Creates an encrypting stream for file encryption (standard modes).
    /// </summary>
    Stream CreateEncryptingStream(Stream outputStream, byte[] key, byte[] iv);

    /// <summary>
    /// Creates a decrypting stream for file decryption (standard modes).
    /// </summary>
    Stream CreateDecryptingStream(Stream inputStream, byte[] key, byte[] iv);

    /// <summary>
    /// Whether this engine supports AEAD operations.
    /// </summary>
    bool SupportsAead { get; }
}
