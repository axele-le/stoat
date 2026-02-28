using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Stoat.Core.ViewModels;
using Stoat.Localization;
using Stoat.Services;
using Stoat.Services.Extensions;
using Stoat.Services.Interfaces;
using Stoat.Services.Services;
using Stoat.Views;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Styling;
using System;
using System.Text.Json;

namespace Stoat;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        try
        {
            Console.WriteLine("Initializing services...");
            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();

            // Initialize profile service (loads/creates profiles)
            var profileService = Services.GetRequiredService<IProfileService>();
            await profileService.InitializeAsync();

            // Initialize PEM storage service (loads existing key pairs)
            var pemStorage = Services.GetRequiredService<IPemStorageService>();
            await pemStorage.InitializeAsync();

            // Initialize API key storage service (loads existing API keys)
            var apiKeyStorage = Services.GetRequiredService<IApiKeyStorageService>();
            await apiKeyStorage.InitializeAsync();

            // Initialize CSR storage service (loads existing CSRs)
            var csrStorage = Services.GetRequiredService<ICsrStorageService>();
            await csrStorage.InitializeAsync();

            // Initialize lock service (loads master password data)
            var lockService = Services.GetRequiredService<ILockService>();
            await lockService.InitializeAsync();

            // Initialize localization and apply saved language BEFORE MainWindow creation
            // so that {l:Translate} bindings evaluate with the correct language from the start
            var locService = Services.GetRequiredService<ILocalizationService>();
            LoadTranslations(locService);

            var clientSettings = await Services.GetRequiredService<IClientSettingsService>().LoadAsync();
            var langCode = LocalizationService.Languages.ElementAtOrDefault(clientSettings.SelectedLanguageIndex)?.Code ?? "it";
            locService.SetLanguage(langCode);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = new MainWindow
                {
                    DataContext = Services.GetRequiredService<MainViewModel>()
                };
                desktop.MainWindow = mainWindow;

                // Wire up clipboard provider now that MainWindow exists
                var clipboardService = Services.GetRequiredService<ClipboardService>();
                clipboardService?.UpdateClipboardProvider(() => TopLevel.GetTopLevel(mainWindow)?.Clipboard);

                // Apply clipboard settings and theme from persisted config
                clipboardService?.UpdateSettings(clientSettings.ClearClipboardEnabled, clientSettings.GetClipboardTimeoutSeconds());
                ApplyTheme(clientSettings.SelectedThemeIndex);

                Console.WriteLine("MainWindow created and assigned");
                mainWindow.Show();
                Console.WriteLine("MainWindow shown");
            }
            Console.WriteLine("App initialization completed.");

            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during app initialization: {ex}");
            throw;
        }
    }

    public void ApplyTheme(int themeIndex)
    {
        RequestedThemeVariant = themeIndex switch
        {
            1 => ThemeVariant.Light,
            2 => ThemeVariant.Default, // Sistema
            _ => ThemeVariant.Dark,
        };
    }

    private static void LoadTranslations(ILocalizationService locService)
    {
        var translations = new Dictionary<string, Dictionary<string, string>>();

        foreach (var lang in LocalizationService.Languages)
        {
            var uri = new Uri($"avares://Stoat/Assets/Localization/{lang.Code}.json");
            try
            {
                using var stream = Avalonia.Platform.AssetLoader.Open(uri);
                using var reader = new System.IO.StreamReader(stream);
                var json = reader.ReadToEnd();
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict != null)
                    translations[lang.Code] = dict;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load {lang.Code}.json: {ex.Message}");
            }
        }

        locService.Initialize(translations);
        LocalizationManager.Instance.Initialize(locService);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Stoat Services (profiles, encryption, PEM, API keys)
        services.AddStoatServices();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<PemViewModel>();
        services.AddTransient<EncryptionViewModel>();
        services.AddTransient<ApiKeyViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<CsrViewModel>();
        services.AddTransient<HashViewModel>();
        services.AddTransient<LockScreenViewModel>();

        // Clipboard service (UI layer — needs Avalonia IClipboard)
        services.AddSingleton<ClipboardService>();
        services.AddSingleton<IClipboardService>(sp => sp.GetRequiredService<ClipboardService>());

        // Confirmation dialog service (UI layer — drives overlay)
        services.AddSingleton<ConfirmationService>();
        services.AddSingleton<IConfirmationService>(sp => sp.GetRequiredService<ConfirmationService>());

        // Toast notification service (UI layer — drives toast overlay)
        services.AddSingleton<ToastService>();
        services.AddSingleton<IToastService>(sp => sp.GetRequiredService<ToastService>());
    }
}
