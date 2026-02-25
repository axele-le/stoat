using Stoat.Services.Interfaces;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Modes;

namespace Stoat.Services.Utils.Engines;

/// <summary>
/// ChaCha20-Poly1305 AEAD cipher engine using BouncyCastle.
/// </summary>
public class ChaCha20Poly1305Engine : ICipherEngine
{
    public bool SupportsAead => true;

    public byte[] Encrypt(byte[] key, byte[] iv, byte[] plaintext)
        => throw new NotSupportedException("ChaCha20-Poly1305 is AEAD only. Use EncryptAead.");

    public byte[] Decrypt(byte[] key, byte[] iv, byte[] ciphertext)
        => throw new NotSupportedException("ChaCha20-Poly1305 is AEAD only. Use DecryptAead.");

    public byte[] EncryptAead(byte[] key, byte[] nonce, byte[] plaintext, byte[]? associatedData = null)
    {
        var cipher = new ChaCha20Poly1305();
        var parameters = CreateParameters(key, nonce);
        cipher.Init(true, parameters);

        if (associatedData != null && associatedData.Length > 0)
            cipher.ProcessAadBytes(associatedData, 0, associatedData.Length);

        var output = new byte[cipher.GetOutputSize(plaintext.Length)];
        var len = cipher.ProcessBytes(plaintext, 0, plaintext.Length, output, 0);
        len += cipher.DoFinal(output, len);

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
        var cipher = new ChaCha20Poly1305();
        var parameters = CreateParameters(key, nonce);
        cipher.Init(false, parameters);

        if (associatedData != null && associatedData.Length > 0)
            cipher.ProcessAadBytes(associatedData, 0, associatedData.Length);

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
        => throw new NotSupportedException("ChaCha20-Poly1305 requires buffered operation. Use EncryptAead.");

    public Stream CreateDecryptingStream(Stream inputStream, byte[] key, byte[] iv)
        => throw new NotSupportedException("ChaCha20-Poly1305 requires buffered operation. Use DecryptAead.");

    private static ParametersWithIV CreateParameters(byte[] key, byte[] nonce)
    {
        return new ParametersWithIV(new KeyParameter(key), nonce);
    }
}
