using Stoat.Services.Interfaces;
using Stoat.Services.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Stoat.Services.Extensions;

/// <summary>
/// Extension methods for registering Stoat services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Stoat profile and cryptographic services to the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddStoatServices(this IServiceCollection services)
    {
        var keyDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Stoat",
            "DataProtection-Keys");

        Directory.CreateDirectory(keyDirectory);

        services.AddDataProtection()
            .SetApplicationName("Stoat")
            .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));

        services.AddSingleton<IApiKeyService, ApiKeyService>();
        services.AddSingleton<IPemService, PemService>();
        services.AddSingleton<ICsrService, CsrService>();
        services.AddSingleton<ICsrImportService, CsrImportService>();
        services.AddSingleton<ISecureStorageService, SecureStorageService>();
        services.AddSingleton<ProfileService>();
        services.AddSingleton<IProfileService>(sp => sp.GetRequiredService<ProfileService>());
        services.AddSingleton<IPemStorageService, PemStorageService>();
        services.AddSingleton<IApiKeyStorageService, ApiKeyStorageService>();
        services.AddSingleton<ICsrStorageService, CsrStorageService>();
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<IHashService, HashService>();
        services.AddSingleton<IClientSettingsService, ClientSettingsService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<LockService>();
        services.AddSingleton<ILockService>(sp => sp.GetRequiredService<LockService>());

        return services;
    }
}
