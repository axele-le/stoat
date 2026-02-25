namespace Stoat.Services.Interfaces;

/// <summary>
/// Service for secure storage using Data Protection API.
/// Handles file storage with automatic protection and password-based encryption for export.
/// </summary>
public interface ISecureStorageService
{
    /// <summary>
    /// Protects a string value using Data Protection API.
    /// The protected value can only be unprotected on the same machine/user.
    /// </summary>
    /// <param name="plaintext">The string to protect.</param>
    /// <returns>The protected string (Base64).</returns>
    string Protect(string plaintext);

    /// <summary>
    /// Unprotects a string value protected by this machine/user.
    /// </summary>
    /// <param name="protectedData">The protected string.</param>
    /// <returns>The original plaintext.</returns>
    string Unprotect(string protectedData);

    /// <summary>
    /// Attempts to unprotect, handling cases where keys may have changed.
    /// </summary>
    /// <param name="protectedData">The protected string.</param>
    /// <param name="plaintext">The decrypted plaintext if successful.</param>
    /// <returns>True if successful, false otherwise.</returns>
    bool TryUnprotect(string protectedData, out string? plaintext);

    /// <summary>
    /// Saves data to a protected file in the app's secure storage location.
    /// </summary>
    /// <typeparam name="T">The type of data to save.</typeparam>
    /// <param name="fileName">The file name (not path).</param>
    /// <param name="data">The data to save.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SaveProtectedFileAsync<T>(string fileName, T data, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Loads data from a protected file.
    /// </summary>
    /// <typeparam name="T">The type of data to load.</typeparam>
    /// <param name="fileName">The file name (not path).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The loaded data, or null if file doesn't exist.</returns>
    Task<T?> LoadProtectedFileAsync<T>(string fileName, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Checks if a protected file exists.
    /// </summary>
    /// <param name="fileName">The file name (not path).</param>
    /// <returns>True if the file exists.</returns>
    bool ProtectedFileExists(string fileName);

    /// <summary>
    /// Deletes a protected file.
    /// </summary>
    /// <param name="fileName">The file name (not path).</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteProtectedFileAsync(string fileName, CancellationToken ct = default);

    /// <summary>
    /// Gets the path to the secure storage directory.
    /// </summary>
    /// <returns>The absolute path to the storage directory.</returns>
    string GetStorageDirectory();

    /// <summary>
    /// Encrypts data using a user-provided password (for export).
    /// Uses AES-256-CBC with PBKDF2 key derivation.
    /// </summary>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <param name="password">The user's password.</param>
    /// <param name="salt">The salt for key derivation (Base64).</param>
    /// <param name="iterations">PBKDF2 iterations (default: 100000).</param>
    /// <returns>The encrypted data (Base64).</returns>
    string EncryptWithPassword(string plaintext, string password, string salt, int iterations = 100000);

    /// <summary>
    /// Decrypts data using a user-provided password (for import).
    /// </summary>
    /// <param name="ciphertext">The encrypted data (Base64).</param>
    /// <param name="password">The user's password.</param>
    /// <param name="salt">The salt used during encryption (Base64).</param>
    /// <param name="iterations">PBKDF2 iterations used during encryption.</param>
    /// <returns>The decrypted plaintext.</returns>
    string DecryptWithPassword(string ciphertext, string password, string salt, int iterations = 100000);

    /// <summary>
    /// Generates a cryptographically random salt for password-based encryption.
    /// </summary>
    /// <param name="length">Number of random bytes (default: 32).</param>
    /// <returns>The salt as Base64 string.</returns>
    string GenerateSalt(int length = 32);

    /// <summary>
    /// Computes HMAC-SHA256 for data integrity verification.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <param name="key">The HMAC key.</param>
    /// <returns>The HMAC as Base64 string.</returns>
    string ComputeHmac(string data, string key);

    /// <summary>
    /// Verifies HMAC-SHA256 for data integrity.
    /// </summary>
    /// <param name="data">The data to verify.</param>
    /// <param name="key">The HMAC key.</param>
    /// <param name="expectedHmac">The expected HMAC (Base64).</param>
    /// <returns>True if HMAC matches, false otherwise.</returns>
    bool VerifyHmac(string data, string key, string expectedHmac);

    /// <summary>
    /// Encrypts data using AES-256-GCM with PBKDF2 key derivation.
    /// GCM provides authenticated encryption (no separate HMAC needed).
    /// Output format: nonce(12) || ciphertext(N) || tag(16) -> Base64.
    /// </summary>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <param name="password">The user's password.</param>
    /// <param name="salt">The salt for key derivation (Base64).</param>
    /// <param name="iterations">PBKDF2 iterations (default: 200000).</param>
    /// <returns>The encrypted data (Base64).</returns>
    string EncryptWithPasswordGcm(string plaintext, string password, string salt, int iterations = 200000);

    /// <summary>
    /// Decrypts data encrypted with AES-256-GCM.
    /// Verifies authentication tag automatically.
    /// </summary>
    /// <param name="ciphertextBase64">The encrypted data (Base64) in format: nonce || ciphertext || tag.</param>
    /// <param name="password">The user's password.</param>
    /// <param name="salt">The salt used during encryption (Base64).</param>
    /// <param name="iterations">PBKDF2 iterations used during encryption.</param>
    /// <returns>The decrypted plaintext.</returns>
    string DecryptWithPasswordGcm(string ciphertextBase64, string password, string salt, int iterations = 200000);
}
