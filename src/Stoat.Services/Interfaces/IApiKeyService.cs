namespace Stoat.Services.Interfaces;

/// <summary>
/// Service for generating cryptographically secure API keys and credentials.
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Generates a new API key as hexadecimal string.
    /// </summary>
    /// <param name="length">Number of random bytes (output will be hex, so 2x length in characters).</param>
    /// <returns>Hexadecimal string.</returns>
    string GenerateApiKey(int length = 32);

    /// <summary>
    /// Generates a URL-safe Base64 API key.
    /// </summary>
    /// <param name="length">Number of random bytes.</param>
    /// <returns>URL-safe Base64 string.</returns>
    string GenerateBase64ApiKey(int length = 32);

    /// <summary>
    /// Generates a secret and salt pair suitable for use as profile credentials.
    /// </summary>
    /// <returns>Tuple containing (secret, salt) both as hex strings.</returns>
    (string secret, string salt) GenerateCredentialPair();
}
