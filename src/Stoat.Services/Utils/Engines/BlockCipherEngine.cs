using Stoat.Services.Enums;
using Stoat.Services.Interfaces;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.IO;

namespace Stoat.Services.Utils.Engines;

/// <summary>
/// Block cipher engine implementing standard (non-AEAD) modes using BouncyCastle.
/// </summary>
public class BlockCipherEngine : ICipherEngine
{
    private readonly SymmetricCipher _cipher;
    private readonly CipherBlockMode _mode;

    public BlockCipherEngine(SymmetricCipher cipher, CipherBlockMode mode)
    {
        _cipher = cipher;
        _mode = mode;
    }

    public bool SupportsAead => false;

    public byte[] Encrypt(byte[] key, byte[] iv, byte[] plaintext)
    {
        var cipher = CreateBufferedCipher(true, key, iv);
        return cipher.DoFinal(plaintext);
    }

    public byte[] Decrypt(byte[] key, byte[] iv, byte[] ciphertext)
    {
        var cipher = CreateBufferedCipher(false, key, iv);
        return cipher.DoFinal(ciphertext);
    }

    public byte[] EncryptAead(byte[] key, byte[] nonce, byte[] plaintext, byte[]? associatedData = null)
        => throw new NotSupportedException("AEAD not supported by BlockCipherEngine. Use AeadCipherEngine.");

    public byte[] DecryptAead(byte[] key, byte[] nonce, byte[] ciphertextWithTag, byte[]? associatedData = null)
        => throw new NotSupportedException("AEAD not supported by BlockCipherEngine. Use AeadCipherEngine.");

    public Stream CreateEncryptingStream(Stream outputStream, byte[] key, byte[] iv)
    {
        var cipher = CreateBufferedCipher(true, key, iv);
        return new CipherStream(outputStream, null, cipher);
    }

    public Stream CreateDecryptingStream(Stream inputStream, byte[] key, byte[] iv)
    {
        var cipher = CreateBufferedCipher(false, key, iv);
        return new CipherStream(inputStream, cipher, null);
    }

    private IBufferedCipher CreateBufferedCipher(bool forEncryption, byte[] key, byte[] iv)
    {
        var engine = CreateBaseEngine();
        var blockCipher = WrapWithMode(engine);

        IBufferedCipher bufferedCipher;

        if (_mode == CipherBlockMode.ECB)
        {
            bufferedCipher = new PaddedBufferedBlockCipher(engine, new Pkcs7Padding());
            bufferedCipher.Init(forEncryption, new KeyParameter(key));
        }
        else if (_mode == CipherBlockMode.CTR || _mode == CipherBlockMode.OFB || _mode == CipherBlockMode.CFB)
        {
            // Stream-like modes don't need padding
            bufferedCipher = new BufferedBlockCipher(blockCipher);
            bufferedCipher.Init(forEncryption, new ParametersWithIV(new KeyParameter(key), iv));
        }
        else
        {
            // CBC needs padding
            bufferedCipher = new PaddedBufferedBlockCipher(blockCipher, new Pkcs7Padding());
            bufferedCipher.Init(forEncryption, new ParametersWithIV(new KeyParameter(key), iv));
        }

        return bufferedCipher;
    }

    private IBlockCipher WrapWithMode(IBlockCipher engine) => _mode switch
    {
        CipherBlockMode.CBC => new CbcBlockCipher(engine),
        CipherBlockMode.CFB => new CfbBlockCipher(engine, engine.GetBlockSize() * 8),
        CipherBlockMode.CTR => new SicBlockCipher(engine),
        CipherBlockMode.OFB => new OfbBlockCipher(engine, engine.GetBlockSize() * 8),
        CipherBlockMode.ECB => engine,
        _ => throw new ArgumentException($"Unsupported block mode for BlockCipherEngine: {_mode}")
    };

    internal static IBlockCipher CreateEngine(SymmetricCipher cipher) => cipher switch
    {
        SymmetricCipher.AES => new AesEngine(),
        SymmetricCipher.Twofish => new TwofishEngine(),
        SymmetricCipher.Serpent => new SerpentEngine(),
        SymmetricCipher.Camellia => new CamelliaEngine(),
        SymmetricCipher.ARIA => new AriaEngine(),
        SymmetricCipher.SM4 => new SM4Engine(),
        SymmetricCipher.SEED => new SeedEngine(),
        SymmetricCipher.Blowfish => new BlowfishEngine(),
        SymmetricCipher.CAST5 => new Cast5Engine(),
        SymmetricCipher.CAST6 => new Cast6Engine(),
        SymmetricCipher.IDEA => new IdeaEngine(),
        SymmetricCipher.TripleDES => new DesEdeEngine(),
        _ => throw new ArgumentException($"Unsupported block cipher: {cipher}")
    };

    private IBlockCipher CreateBaseEngine() => CreateEngine(_cipher);
}
