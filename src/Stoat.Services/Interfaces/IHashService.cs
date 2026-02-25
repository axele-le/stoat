using Stoat.Services.Enums;

namespace Stoat.Services.Interfaces;

/// <summary>
/// Service for standalone hashing operations.
/// </summary>
public interface IHashService
{
    /// <summary>
    /// Computes a hash of the given text using the specified algorithm.
    /// </summary>
    string HashText(string input, HashAlgorithmType algorithm);

    /// <summary>
    /// Computes a hash of the given file using the specified algorithm.
    /// </summary>
    Task<string> HashFileAsync(string filePath, HashAlgorithmType algorithm, IProgress<double>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// Verifies a hash against a given text.
    /// </summary>
    bool VerifyTextHash(string input, string expectedHash, HashAlgorithmType algorithm);

    /// <summary>
    /// Verifies a hash against a given file.
    /// </summary>
    Task<bool> VerifyFileHashAsync(string filePath, string expectedHash, HashAlgorithmType algorithm, CancellationToken ct = default);
}
