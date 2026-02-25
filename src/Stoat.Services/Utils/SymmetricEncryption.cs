using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Stoat.Services.Enums;
using Stoat.Services.Models;
using Stoat.Services.Utils.KeyDerivers;

namespace Stoat.Services.Utils;

/// <summary>
/// Unified entry point for symmetric encryption/decryption.
/// Supports all cipher/mode/KDF combinations via CipherEngineFactory and KeyDeriverFactory.
/// </summary>
public static class SymmetricEncryption
{
    private const byte CurrentFileFormatVersion = 1;
    public const string EncryptedFileExtension = ".ccgenc";

    /// <summary>
    /// Encrypts text using the provided encryption settings.
    /// </summary>
    public static string Encrypt(string secret, string salt, string value, EncryptionSettings settings)
    {
        var passwordBytes = SHA256.HashData(Encoding.UTF8.GetBytes(secret.Trim()));
        var saltBytes = Encoding.ASCII.GetBytes(salt.Trim());
        var plaintext = Encoding.UTF8.GetBytes(value);

        var keySizeBytes = settings.KeySizeBits / 8;
        var ivSizeBytes = CipherMetadata.GetIvSizeBytes(settings.Cipher, settings.Mode);

        var deriver = KeyDeriverFactory.Create(settings);
        var (key, derivedIv) = deriver.DeriveKey(passwordBytes, saltBytes, keySizeBytes, ivSizeBytes);

        var engine = CipherEngineFactory.Create(settings.Cipher, settings.Mode);

        byte[] encrypted;
        if (engine.SupportsAead || settings.IsStreamCipher)
        {
            // AEAD: generate random nonce, prepend to output
            var nonce = RandomNumberGenerator.GetBytes(ivSizeBytes);
            var ciphertext = engine.EncryptAead(key, nonce, plaintext);
            encrypted = new byte[nonce.Length + ciphertext.Length];
            Array.Copy(nonce, 0, encrypted, 0, nonce.Length);
            Array.Copy(ciphertext, 0, encrypted, nonce.Length, ciphertext.Length);
        }
        else
        {
            encrypted = engine.Encrypt(key, derivedIv, plaintext);
        }

        return Convert.ToBase64String(encrypted);
    }

    /// <summary>
    /// Decrypts text using the provided encryption settings.
    /// </summary>
    public static string Decrypt(string secret, string salt, string value, EncryptionSettings settings)
    {
        var passwordBytes = SHA256.HashData(Encoding.UTF8.GetBytes(secret.Trim()));
        var saltBytes = Encoding.ASCII.GetBytes(salt.Trim());
        var allBytes = Convert.FromBase64String(value.Trim().Replace("\r", "").Replace("\n", "").Replace(" ", ""));

        var keySizeBytes = settings.KeySizeBits / 8;
        var ivSizeBytes = CipherMetadata.GetIvSizeBytes(settings.Cipher, settings.Mode);

        var deriver = KeyDeriverFactory.Create(settings);
        var (key, derivedIv) = deriver.DeriveKey(passwordBytes, saltBytes, keySizeBytes, ivSizeBytes);

        var engine = CipherEngineFactory.Create(settings.Cipher, settings.Mode);

        byte[] decrypted;
        if (engine.SupportsAead || settings.IsStreamCipher)
        {
            // AEAD: nonce is prepended to the ciphertext
            var nonce = new byte[ivSizeBytes];
            Array.Copy(allBytes, 0, nonce, 0, ivSizeBytes);
            var ciphertextWithTag = new byte[allBytes.Length - ivSizeBytes];
            Array.Copy(allBytes, ivSizeBytes, ciphertextWithTag, 0, ciphertextWithTag.Length);
            decrypted = engine.DecryptAead(key, nonce, ciphertextWithTag);
        }
        else
        {
            decrypted = engine.Decrypt(key, derivedIv, allBytes);
        }

        return Encoding.UTF8.GetString(decrypted);
    }

    /// <summary>
    /// Encrypts a file using the current format.
    /// </summary>
    public static async Task EncryptFileAsync(string secret, string salt, string inputPath, string outputPath,
        EncryptionSettings settings, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var passwordBytes = SHA256.HashData(Encoding.UTF8.GetBytes(secret.Trim()));
        var saltBytes = Encoding.ASCII.GetBytes(salt.Trim());

        var keySizeBytes = settings.KeySizeBits / 8;
        var ivSizeBytes = CipherMetadata.GetIvSizeBytes(settings.Cipher, settings.Mode);

        var deriver = KeyDeriverFactory.Create(settings);
        var (key, _) = deriver.DeriveKey(passwordBytes, saltBytes, keySizeBytes, ivSizeBytes);

        // Generate random IV/nonce
        var nonce = RandomNumberGenerator.GetBytes(ivSizeBytes);

        var fileInfo = new FileInfo(inputPath);
        var totalBytes = fileInfo.Length;
        var processedBytes = 0L;

        var originalFileName = Path.GetFileName(inputPath);
        var fileNameBytes = Encoding.UTF8.GetBytes(originalFileName);

        // Serialize settings to JSON
        var settingsJson = JsonSerializer.SerializeToUtf8Bytes(settings);

        await using var inputStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        await using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

        // Write file header:
        // [1 byte version]
        // [2 bytes settings length]
        // [N bytes JSON settings]
        // [4 bytes filename length]
        // [filename bytes]
        // [IV/nonce bytes]
        // [encrypted data]
        await outputStream.WriteAsync(new[] { CurrentFileFormatVersion }, ct);
        await outputStream.WriteAsync(BitConverter.GetBytes((ushort)settingsJson.Length), ct);
        await outputStream.WriteAsync(settingsJson, ct);
        await outputStream.WriteAsync(BitConverter.GetBytes(fileNameBytes.Length), ct);
        await outputStream.WriteAsync(fileNameBytes, ct);
        await outputStream.WriteAsync(nonce, ct);

        var engine = CipherEngineFactory.Create(settings.Cipher, settings.Mode);

        if (engine.SupportsAead || settings.IsStreamCipher)
        {
            // AEAD: buffer entire file, encrypt, write
            var plaintext = new byte[totalBytes];
            var read = await inputStream.ReadAsync(plaintext, ct);
            if (read < totalBytes)
            {
                Array.Resize(ref plaintext, read);
            }

            var ciphertext = engine.EncryptAead(key, nonce, plaintext);
            await outputStream.WriteAsync(ciphertext, ct);
            progress?.Report(100);
        }
        else
        {
            // Stream-based encryption for non-AEAD modes
            await using var cryptoStream = engine.CreateEncryptingStream(outputStream, key, nonce);
            var buffer = new byte[81920];
            int bytesRead;
            while ((bytesRead = await inputStream.ReadAsync(buffer, ct)) > 0)
            {
                ct.ThrowIfCancellationRequested();
                await cryptoStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                processedBytes += bytesRead;
                progress?.Report((double)processedBytes / totalBytes * 100);
            }
        }
    }

    /// <summary>
    /// Decrypts an encrypted file. Returns the output file path.
    /// </summary>
    public static async Task<string> DecryptFileAsync(string secret, string salt, string inputPath, string outputDirectory,
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var passwordBytes = SHA256.HashData(Encoding.UTF8.GetBytes(secret.Trim()));
        var saltBytes = Encoding.ASCII.GetBytes(salt.Trim());

        await using var inputStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);

        var fileInfo = new FileInfo(inputPath);
        var totalBytes = fileInfo.Length;

        // Read version byte
        var versionByte = new byte[1];
        await inputStream.ReadExactlyAsync(versionByte, ct);

        if (versionByte[0] != CurrentFileFormatVersion)
            throw new InvalidDataException($"Unsupported file format version: {versionByte[0]}");

        // Read settings length
        var settingsLengthBytes = new byte[2];
        await inputStream.ReadExactlyAsync(settingsLengthBytes, ct);
        var settingsLength = BitConverter.ToUInt16(settingsLengthBytes);

        // Read settings JSON
        var settingsBytes = new byte[settingsLength];
        await inputStream.ReadExactlyAsync(settingsBytes, ct);
        var settings = JsonSerializer.Deserialize<EncryptionSettings>(settingsBytes)
            ?? throw new InvalidDataException("Failed to deserialize encryption settings");

        // Read filename
        var fileNameLengthBytes = new byte[4];
        await inputStream.ReadExactlyAsync(fileNameLengthBytes, ct);
        var fileNameLength = BitConverter.ToInt32(fileNameLengthBytes);

        if (fileNameLength <= 0 || fileNameLength > 260)
            throw new InvalidDataException("Invalid filename length in file header");

        var fileNameBytes = new byte[fileNameLength];
        await inputStream.ReadExactlyAsync(fileNameBytes, ct);
        var originalFileName = Encoding.UTF8.GetString(fileNameBytes);

        // Read nonce/IV
        var ivSizeBytes = CipherMetadata.GetIvSizeBytes(settings.Cipher, settings.Mode);
        var nonce = new byte[ivSizeBytes];
        await inputStream.ReadExactlyAsync(nonce, ct);

        // Derive key
        var keySizeBytes = settings.KeySizeBits / 8;
        var deriver = KeyDeriverFactory.Create(settings);
        var (key, _) = deriver.DeriveKey(passwordBytes, saltBytes, keySizeBytes, ivSizeBytes);

        // Determine output path
        var outputPath = Path.Combine(outputDirectory, originalFileName);
        var counter = 1;
        var baseName = Path.GetFileNameWithoutExtension(originalFileName);
        var extension = Path.GetExtension(originalFileName);
        while (File.Exists(outputPath))
        {
            outputPath = Path.Combine(outputDirectory, $"{baseName}_{counter}{extension}");
            counter++;
        }

        var engine = CipherEngineFactory.Create(settings.Cipher, settings.Mode);

        if (engine.SupportsAead || settings.IsStreamCipher)
        {
            // AEAD: read all remaining data, decrypt
            var remainingLength = (int)(inputStream.Length - inputStream.Position);
            var ciphertextWithTag = new byte[remainingLength];
            await inputStream.ReadExactlyAsync(ciphertextWithTag, ct);

            var plaintext = engine.DecryptAead(key, nonce, ciphertextWithTag);
            await File.WriteAllBytesAsync(outputPath, plaintext, ct);
            progress?.Report(100);
        }
        else
        {
            // Stream-based decryption
            await using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
            await using var cryptoStream = engine.CreateDecryptingStream(inputStream, key, nonce);
            var buffer = new byte[81920];
            int bytesRead;
            var processedBytes = 0L;
            while ((bytesRead = await cryptoStream.ReadAsync(buffer, ct)) > 0)
            {
                ct.ThrowIfCancellationRequested();
                await outputStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                processedBytes += bytesRead;
                progress?.Report((double)processedBytes / totalBytes * 100);
            }
        }

        return outputPath;
    }

    /// <summary>
    /// Detects the file format version from the first byte.
    /// </summary>
    public static async Task<byte> DetectFileVersion(string inputPath, CancellationToken ct = default)
    {
        await using var stream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var versionByte = new byte[1];
        await stream.ReadExactlyAsync(versionByte, ct);
        return versionByte[0];
    }
}
