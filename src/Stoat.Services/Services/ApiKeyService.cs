using System.Security.Cryptography;
using Stoat.Services.Interfaces;

namespace Stoat.Services.Services;

/// <summary>
/// Service for generating cryptographically secure API keys and credentials.
/// </summary>
public class ApiKeyService : IApiKeyService
{
    /// <inheritdoc />
    public string GenerateApiKey(int length = 32)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be positive.");

        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }

    /// <inheritdoc />
    public string GenerateBase64ApiKey(int length = 32)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be positive.");

        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);

        // URL-safe Base64: replace + with -, / with _, remove padding
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <inheritdoc />
    public (string secret, string salt) GenerateCredentialPair()
    {
        // 32 bytes (256-bit) secret
        var secret = GenerateApiKey(32);

        // 16 bytes salt
        var salt = GenerateApiKey(16);

        return (secret, salt);
    }
}
