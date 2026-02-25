using Stoat.Services.Models;

namespace Stoat.Services.Interfaces;

/// <summary>
/// Service for managing API key storage with DPAPI protection.
/// Keys are isolated per profile (multi-tenancy).
/// </summary>
public interface IApiKeyStorageService
{
    /// <summary>
    /// Initializes the service by loading existing API keys from storage.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all stored API keys for the active profile.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of API keys.</returns>
    Task<IReadOnlyList<ApiKey>> GetAllKeysAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a specific API key by its ID (only if it belongs to the active profile).
    /// </summary>
    /// <param name="id">The API key ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API key if found, null otherwise.</returns>
    Task<ApiKey?> GetKeyByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates and stores a new API key.
    /// </summary>
    /// <param name="name">Human-readable name for the API key.</param>
    /// <param name="type">The type of API key (Development or Production).</param>
    /// <param name="length">Length of the key in characters.</param>
    /// <param name="complexity">Complexity level for character set.</param>
    /// <param name="prefix">Optional prefix to prepend to the key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created API key.</returns>
    Task<ApiKey> CreateKeyAsync(string name, ApiKeyType type, int length = 32, ApiKeyComplexity complexity = ApiKeyComplexity.AlphanumericSymbols, string? prefix = null, CancellationToken ct = default);

    /// <summary>
    /// Deletes an API key by its ID.
    /// </summary>
    /// <param name="id">The API key ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteKeyAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets the decrypted value of an API key.
    /// </summary>
    /// <param name="id">The API key ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The decrypted API key value.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the API key is not found.</exception>
    Task<string> GetDecryptedValueAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Raised when a new API key is created.
    /// </summary>
    event EventHandler<ApiKey>? KeyCreated;

    /// <summary>
    /// Raised when an API key is deleted.
    /// </summary>
    event EventHandler<Guid>? KeyDeleted;
}
