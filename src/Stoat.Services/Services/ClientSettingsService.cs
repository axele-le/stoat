using Stoat.Services.Interfaces;
using Stoat.Services.Models;

namespace Stoat.Services.Services;

/// <summary>
/// Persists client settings as a DPAPI-protected file.
/// </summary>
public class ClientSettingsService : IClientSettingsService
{
    private readonly ISecureStorageService _storage;
    private const string SettingsFileName = "client-settings.dat";

    public ClientSettingsService(ISecureStorageService storage)
    {
        _storage = storage;
    }

    public async Task<ClientSettings> LoadAsync(CancellationToken ct = default)
    {
        var settings = await _storage.LoadProtectedFileAsync<ClientSettings>(SettingsFileName, ct);
        return settings ?? new ClientSettings();
    }

    public async Task SaveAsync(ClientSettings settings, CancellationToken ct = default)
    {
        await _storage.SaveProtectedFileAsync(SettingsFileName, settings, ct);
    }
}
