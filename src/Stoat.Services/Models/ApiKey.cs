namespace Stoat.Services.Models;

/// <summary>
/// Type of API key environment.
/// </summary>
public enum ApiKeyType
{
    /// <summary>
    /// Development environment key.
    /// </summary>
    Development,

    /// <summary>
    /// Test environment key.
    /// </summary>
    Test,

    /// <summary>
    /// Production environment key.
    /// </summary>
    Production
}

/// <summary>
/// Complexity level for API key generation.
/// </summary>
public enum ApiKeyComplexity
{
    /// <summary>
    /// Only numeric characters (0-9).
    /// </summary>
    Numeric,

    /// <summary>
    /// Lowercase letters and numbers.
    /// </summary>
    AlphanumericLower,

    /// <summary>
    /// Mixed case letters and numbers.
    /// </summary>
    Alphanumeric,

    /// <summary>
    /// Letters, numbers, and symbols.
    /// </summary>
    AlphanumericSymbols,

    /// <summary>
    /// Base64 URL-safe encoding (A-Z, a-z, 0-9, -, _).
    /// </summary>
    Base64UrlSafe
}

/// <summary>
/// Represents a stored API key with metadata.
/// </summary>
public class ApiKey
{
    /// <summary>
    /// Unique identifier for the API key.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The profile this API key belongs to (multi-tenancy support).
    /// </summary>
    public Guid ProfileId { get; set; }

    /// <summary>
    /// Human-readable name for the API key.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of the API key (Development or Production).
    /// </summary>
    public ApiKeyType Type { get; set; } = ApiKeyType.Development;

    /// <summary>
    /// Length of the generated key in characters.
    /// </summary>
    public int Length { get; set; } = 32;

    /// <summary>
    /// Complexity level used for generation.
    /// </summary>
    public ApiKeyComplexity Complexity { get; set; } = ApiKeyComplexity.AlphanumericSymbols;

    /// <summary>
    /// The actual API key value (stored protected with DPAPI).
    /// </summary>
    public string ProtectedValue { get; set; } = string.Empty;

    /// <summary>
    /// Prefix used for the key (e.g., "cv_dev_", "cv_prod_").
    /// </summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the API key was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Version number for optimistic concurrency and migration support.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets a masked preview of the key value (e.g., "cv_prod_abc1...xyz9").
    /// </summary>
    public string GetMaskedPreview(string decryptedValue)
    {
        if (string.IsNullOrEmpty(decryptedValue) || decryptedValue.Length < 16)
            return new string('\u2022', 20);

        var start = decryptedValue[..Math.Min(12, decryptedValue.Length / 3)];
        var end = decryptedValue[^Math.Min(8, decryptedValue.Length / 4)..];
        return $"{start}...{end}";
    }
}
