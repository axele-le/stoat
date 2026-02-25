using Stoat.Services.Models;

namespace Stoat.Services.Interfaces;

/// <summary>
/// Service for loading and saving client settings (DPAPI-protected).
/// </summary>
public interface IClientSettingsService
{
    /// <summary>
    /// Loads client settings from protected storage.
    /// Returns default settings if no file exists.
    /// </summary>
    Task<ClientSettings> LoadAsync(CancellationToken ct = default);

    /// <summary>
    /// Saves client settings to protected storage.
    /// </summary>
    Task SaveAsync(ClientSettings settings, CancellationToken ct = default);
}
