using Stoat.Services.Enums;
using Stoat.Services.Interfaces;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Stoat.Services.Utils.Engines;

/// <summary>
/// AEAD cipher engine supporting GCM, CCM, and EAX modes using BouncyCastle.
/// </summary>
public class AeadCipherEngine : ICipherEngine
{
    private readonly SymmetricCipher _cipher;
    private readonly CipherBlockMode _mode;
    private const int TagSizeBits = 128;

    public AeadCipherEngine(SymmetricCipher cipher, CipherBlockMode mode)
    {
        _cipher = cipher;
        _mode = mode;

        if (mode is not (CipherBlockMode.GCM or CipherBlockMode.CCM or CipherBlockMode.EAX))
            throw new ArgumentException($"AeadCipherEngine only supports GCM, CCM, EAX. Got: {mode}");
    }

    public bool SupportsAead => true;

    public byte[] Encrypt(byte[] key, byte[] iv, byte[] plaintext)
        => throw new NotSupportedException("Use EncryptAead for AEAD modes.");

    public byte[] Decrypt(byte[] key, byte[] iv, byte[] ciphertext)
        => throw new NotSupportedException("Use DecryptAead for AEAD modes.");

    public byte[] EncryptAead(byte[] key, byte[] nonce, byte[] plaintext, byte[]? associatedData = null)
    {
        var cipher = CreateAeadCipher();
        var parameters = CreateAeadParameters(key, nonce, associatedData);
        cipher.Init(true, parameters);

        var output = new byte[cipher.GetOutputSize(plaintext.Length)];
        var len = cipher.ProcessBytes(plaintext, 0, plaintext.Length, output, 0);
        len += cipher.DoFinal(output, len);

        // Trim to actual length
        if (len < output.Length)
        {
            var trimmed = new byte[len];
            Array.Copy(output, trimmed, len);
            return trimmed;
        }

        return output;
    }

    public byte[] DecryptAead(byte[] key, byte[] nonce, byte[] ciphertextWithTag, byte[]? associatedData = null)
    {
        var cipher = CreateAeadCipher();
        var parameters = CreateAeadParameters(key, nonce, associatedData);
        cipher.Init(false, parameters);

        var output = new byte[cipher.GetOutputSize(ciphertextWithTag.Length)];
        var len = cipher.ProcessBytes(ciphertextWithTag, 0, ciphertextWithTag.Length, output, 0);
        len += cipher.DoFinal(output, len);

        if (len < output.Length)
        {
            var trimmed = new byte[len];
            Array.Copy(output, trimmed, len);
            return trimmed;
        }

        return output;
    }

    public Stream CreateEncryptingStream(Stream outputStream, byte[] key, byte[] iv)
        => throw new NotSupportedException("AEAD modes require buffered operation for tag computation. Use EncryptAead instead.");

    public Stream CreateDecryptingStream(Stream inputStream, byte[] key, byte[] iv)
        => throw new NotSupportedException("AEAD modes require buffered operation for tag verification. Use DecryptAead instead.");

    private IAeadBlockCipher CreateAeadCipher()
    {
        var engine = BlockCipherEngine.CreateEngine(_cipher);
        return _mode switch
        {
            CipherBlockMode.GCM => new GcmBlockCipher(engine),
            CipherBlockMode.CCM => new CcmBlockCipher(engine),
            CipherBlockMode.EAX => new EaxBlockCipher(engine),
            _ => throw new ArgumentException($"Unsupported AEAD mode: {_mode}")
        };
    }

    private ICipherParameters CreateAeadParameters(byte[] key, byte[] nonce, byte[]? associatedData)
    {
        var keyParam = new KeyParameter(key);
        if (associatedData != null && associatedData.Length > 0)
        {
            return new AeadParameters(keyParam, TagSizeBits, nonce, associatedData);
        }
        return new AeadParameters(keyParam, TagSizeBits, nonce);
    }
}
