using Stoat.Services.Models;

namespace Stoat.Services.Interfaces;

/// <summary>
/// Service for encryption/decryption operations using the active profile's credentials.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a value using the active profile's credentials.
    /// </summary>
    string Encrypt(string plaintext, EncryptionSettings settings);

    /// <summary>
    /// Decrypts a value using the active profile's credentials.
    /// </summary>
    string Decrypt(string ciphertext, EncryptionSettings settings);

    /// <summary>
    /// Attempts to encrypt, returning success status and result.
    /// </summary>
    bool TryEncrypt(string plaintext, out string? ciphertext, out string? error, EncryptionSettings settings);

    /// <summary>
    /// Attempts to decrypt, returning success status and result.
    /// </summary>
    bool TryDecrypt(string ciphertext, out string? plaintext, out string? error, EncryptionSettings settings);

    /// <summary>
    /// Encrypts a file using the active profile's credentials.
    /// </summary>
    Task<string> EncryptFileAsync(string inputPath, string? outputPath = null, EncryptionSettings? settings = null,
        IProgress<double>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// Decrypts a file using the active profile's credentials.
    /// </summary>
    Task<string> DecryptFileAsync(string inputPath, string? outputDirectory = null,
        IProgress<double>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// Attempts to encrypt a file.
    /// </summary>
    Task<(bool success, string? resultPath, string? error)> TryEncryptFileAsync(string inputPath, string? outputPath = null,
        EncryptionSettings? settings = null, IProgress<double>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// Attempts to decrypt a file.
    /// </summary>
    Task<(bool success, string? resultPath, string? error)> TryDecryptFileAsync(string inputPath, string? outputDirectory = null,
        IProgress<double>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// Gets the default file extension for encrypted files.
    /// </summary>
    string EncryptedFileExtension { get; }
}
