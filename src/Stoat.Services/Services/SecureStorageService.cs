using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Stoat.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace Stoat.Services.Services;

/// <summary>
/// Service for secure storage using Data Protection API.
/// Handles file storage with automatic protection and password-based encryption for export.
/// </summary>
public class SecureStorageService : ISecureStorageService
{
    private readonly IDataProtector _protector;
    private readonly string _storageDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    // Purpose string for Data Protection - identifies this application's data
    private const string DataProtectionPurpose = "Stoat.ProfileStorage.v1";

    public SecureStorageService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(DataProtectionPurpose);
        _storageDirectory = GetStorageDirectory();
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Ensure storage directory exists
        Directory.CreateDirectory(_storageDirectory);
    }

    /// <inheritdoc />
    public string GetStorageDirectory()
    {
        // Cross-platform: Use LocalApplicationData
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "Stoat", "Profiles");
    }

    /// <inheritdoc />
    public string Protect(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            return plaintext;

        return _protector.Protect(plaintext);
    }

    /// <inheritdoc />
    public string Unprotect(string protectedData)
    {
        if (string.IsNullOrEmpty(protectedData))
            return protectedData;

        return _protector.Unprotect(protectedData);
    }

    /// <inheritdoc />
    public bool TryUnprotect(string protectedData, out string? plaintext)
    {
        plaintext = null;

        if (string.IsNullOrEmpty(protectedData))
        {
            plaintext = protectedData;
            return true;
        }

        try
        {
            plaintext = _protector.Unprotect(protectedData);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task SaveProtectedFileAsync<T>(string fileName, T data, CancellationToken ct = default) where T : class
    {
        var filePath = Path.Combine(_storageDirectory, fileName);
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var protectedJson = Protect(json);

        // Write to temp file first, then rename (atomic operation)
        var tempPath = filePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, protectedJson, ct);
        File.Move(tempPath, filePath, overwrite: true);
    }

    /// <inheritdoc />
    public async Task<T?> LoadProtectedFileAsync<T>(string fileName, CancellationToken ct = default) where T : class
    {
        var filePath = Path.Combine(_storageDirectory, fileName);
        if (!File.Exists(filePath))
            return null;

        var protectedJson = await File.ReadAllTextAsync(filePath, ct);
        var json = Unprotect(protectedJson);
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    /// <inheritdoc />
    public bool ProtectedFileExists(string fileName)
    {
        return File.Exists(Path.Combine(_storageDirectory, fileName));
    }

    /// <inheritdoc />
    public Task DeleteProtectedFileAsync(string fileName, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_storageDirectory, fileName);
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public string EncryptWithPassword(string plaintext, string password, string salt, int iterations = 100000)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;

        // Derive key and IV from password using PBKDF2
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, iterations, HashAlgorithmName.SHA256);
        aes.Key = pbkdf2.GetBytes(32);  // 256 bits
        aes.IV = pbkdf2.GetBytes(16);   // 128 bits

        using var encryptor = aes.CreateEncryptor();
        var encryptedBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        return Convert.ToBase64String(encryptedBytes);
    }

    /// <inheritdoc />
    public string DecryptWithPassword(string ciphertext, string password, string salt, int iterations = 100000)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var ciphertextBytes = Convert.FromBase64String(ciphertext);

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;

        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, iterations, HashAlgorithmName.SHA256);
        aes.Key = pbkdf2.GetBytes(32);
        aes.IV = pbkdf2.GetBytes(16);

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(ciphertextBytes, 0, ciphertextBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    /// <inheritdoc />
    public string GenerateSalt(int length = 32)
    {
        var saltBytes = new byte[length];
        RandomNumberGenerator.Fill(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    /// <inheritdoc />
    public string ComputeHmac(string data, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);
        return Convert.ToBase64String(hashBytes);
    }

    /// <inheritdoc />
    public bool VerifyHmac(string data, string key, string expectedHmac)
    {
        var computedHmac = ComputeHmac(data, key);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHmac),
            Encoding.UTF8.GetBytes(expectedHmac));
    }

    /// <inheritdoc />
    public string EncryptWithPasswordGcm(string plaintext, string password, string salt, int iterations = 200000)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

        // Derive 256-bit key from password
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, iterations, HashAlgorithmName.SHA256);
        var key = pbkdf2.GetBytes(32);

        // Generate random 12-byte nonce
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes

        using var aesGcm = new AesGcm(key, tag.Length);
        aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Output: nonce(12) || ciphertext(N) || tag(16)
        var result = new byte[nonce.Length + ciphertext.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(ciphertext, 0, result, nonce.Length, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length + ciphertext.Length, tag.Length);

        return Convert.ToBase64String(result);
    }

    /// <inheritdoc />
    public string DecryptWithPasswordGcm(string ciphertextBase64, string password, string salt, int iterations = 200000)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var combined = Convert.FromBase64String(ciphertextBase64);

        const int nonceSize = 12;
        const int tagSize = 16;

        if (combined.Length < nonceSize + tagSize)
            throw new CryptographicException("Invalid ciphertext: data too short.");

        // Parse nonce(12) || ciphertext(N) || tag(16)
        var nonce = new byte[nonceSize];
        var ciphertextLength = combined.Length - nonceSize - tagSize;
        var ciphertext = new byte[ciphertextLength];
        var tag = new byte[tagSize];

        Buffer.BlockCopy(combined, 0, nonce, 0, nonceSize);
        Buffer.BlockCopy(combined, nonceSize, ciphertext, 0, ciphertextLength);
        Buffer.BlockCopy(combined, nonceSize + ciphertextLength, tag, 0, tagSize);

        // Derive key
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, iterations, HashAlgorithmName.SHA256);
        var key = pbkdf2.GetBytes(32);

        var plaintext = new byte[ciphertextLength];
        using var aesGcm = new AesGcm(key, tagSize);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
