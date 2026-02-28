using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Stoat.Localization;
using Stoat.Services;
using Stoat.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Stoat.Views;

public partial class MainWindow : Window
{
    private bool _sidebarExpanded = false;
    private const double SidebarCollapsedWidth = 62;
    private const double SidebarExpandedWidth = 180;
    private IProfileService? _profileService;
    private ILockService? _lockService;
    private IClientSettingsService? _clientSettingsService;
    private ConfirmationService? _confirmationService;
    private ToastService? _toastService;
    private ILocalizationService? _localizationService;

    private static readonly WindowIcon IconDark = new(Avalonia.Platform.AssetLoader.Open(new Uri("avares://Stoat/Assets/Images/stoat-white.ico")));
    private static readonly WindowIcon IconLight = new(Avalonia.Platform.AssetLoader.Open(new Uri("avares://Stoat/Assets/Images/stoat.ico")));

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }


    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        MinHeight = 800;
        MinWidth = 1200;

        // Set version text
        var version = Assembly.GetExecutingAssembly()
                      .GetName()
                      .Version;
        var versionText = this.FindControl<TextBlock>("VersionText");
        if (versionText != null && version != null)
        {
            versionText.Text = $"v{version.Major}.{version.Minor}.{version.Build}";
        }

        _profileService = App.Services?.GetService<IProfileService>();
        if (_profileService != null)
        {
            _profileService.ActiveProfileChanged += OnActiveProfileChanged;
            await RefreshProfileDropdown();
        }

        // Initialize lock service
        _lockService = App.Services?.GetService<ILockService>();
        _clientSettingsService = App.Services?.GetService<IClientSettingsService>();

        if (_lockService != null)
        {
            _lockService.LockStateChanged += OnLockStateChanged;
            await SetupAutoLockAsync();
        }

        var clipboardService = App.Services?.GetService<IClipboardService>();
        if (clipboardService != null)
        {
            clipboardService.ClipboardCleared += OnClipboardCleared;
        }

        _toastService = App.Services?.GetService<ToastService>();
        if (_toastService != null)
        {
            var toastContainer = this.FindControl<ItemsControl>("ToastContainer");
            if (toastContainer != null)
            {
                toastContainer.ItemsSource = _toastService.Toasts;
            }
        }

        _confirmationService = App.Services?.GetService<ConfirmationService>();
        if (_confirmationService != null)
        {
            _confirmationService.PropertyChanged += OnConfirmationPropertyChanged;

            var confirmBtn = this.FindControl<Button>("ConfirmationConfirmButton");
            var cancelBtn = this.FindControl<Button>("ConfirmationCancelButton");
            if (confirmBtn != null) confirmBtn.Click += (_, _) => _confirmationService.OnConfirmed();
            if (cancelBtn != null) cancelBtn.Click += (_, _) => _confirmationService.OnCancelled();
        }

        _localizationService = App.Services?.GetService<ILocalizationService>();

        PointerMoved += OnUserActivity;
        KeyDown += OnUserActivity;
        PointerPressed += OnUserActivity;

        // Set logo based on current theme and subscribe to changes
        UpdateLogo();
        ActualThemeVariantChanged += (_, _) => UpdateLogo();

        // Set Home as default view
        var content = this.FindControl<ContentControl>("MainContent");
        if (content != null)
        {
            content.Content = new HomeView();
        }
    }

    private void UpdateLogo()
    {
        var isDark = ActualThemeVariant == ThemeVariant.Dark;
        var logoDark = this.FindControl<Image>("LogoDark");
        var logoLight = this.FindControl<Image>("LogoLight");
        if (logoDark != null) logoDark.IsVisible = isDark;
        if (logoLight != null) logoLight.IsVisible = !isDark;

        Icon = isDark ? IconDark : IconLight;
    }

    private void OnUserActivity(object? sender, EventArgs e)
    {
        _lockService?.ResetActivityTimer();
    }

    private void OnClipboardCleared(object? sender, EventArgs e)
    {
        var message = _localizationService?.Format("MainWindow.Notification.ClipboardCleared") ?? string.Empty;
        _toastService?.Success(message);
    }

    private void OnConfirmationPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_confirmationService == null) return;

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var overlay = this.FindControl<Border>("ConfirmationOverlay");
            var title = this.FindControl<TextBlock>("ConfirmationTitle");
            var message = this.FindControl<TextBlock>("ConfirmationMessage");

            if (overlay != null) overlay.IsVisible = _confirmationService.IsVisible;
            if (title != null) title.Text = _confirmationService.Title;
            if (message != null) message.Text = _confirmationService.Message;
        });
    }

    private void OnLockStateChanged(object? sender, bool isLocked)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var overlay = this.FindControl<LockScreenOverlay>("LockOverlay");
            if (overlay != null)
            {
                overlay.IsVisible = isLocked;
            }
        });
    }

    private async Task SetupAutoLockAsync()
    {
        if (_clientSettingsService == null || _lockService == null) return;

        var settings = await _clientSettingsService.LoadAsync();
        if (settings.AutoLockEnabled && _lockService.HasMasterPassword)
        {
            // Lock on startup: user must authenticate before accessing data
            await _lockService.LockAsync();
            _lockService.StartActivityTimer(settings.GetLockTimeoutMinutes());
        }
    }

    /// <summary>
    /// Called by SettingsViewModel (via event) when auto-lock settings change.
    /// Receives current values directly to avoid race conditions with debounced save.
    /// </summary>
    public void OnAutoLockSettingsChanged(object? sender, Core.ViewModels.AutoLockSettingsEventArgs e)
    {
        if (_lockService == null) return;

        if (e.Enabled && _lockService.HasMasterPassword)
        {
            _lockService.StartActivityTimer(e.TimeoutMinutes);
        }
        else
        {
            _lockService.StopActivityTimer();
        }
    }

    private void OnActiveProfileChanged(object? sender, Services.Interfaces.ProfileChangedEventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(async () => await RefreshProfileDropdown());
    }

    private async Task RefreshProfileDropdown()
    {
        if (_profileService == null) return;

        var profiles = await _profileService.GetAllProfilesAsync();
        var activeProfile = _profileService.ActiveProfile;

        // Update header text
        var profileNameText = this.FindControl<TextBlock>("ProfileNameText");
        if (profileNameText != null && activeProfile != null)
        {
            profileNameText.Text = activeProfile.Name;
        }

        // Rebuild dropdown list
        var listPanel = this.FindControl<StackPanel>("ProfileListPanel");
        if (listPanel == null) return;

        // Remove all except the last two elements (Separator and "Gestisci profili" button)
        while (listPanel.Children.Count > 2)
        {
            listPanel.Children.RemoveAt(0);
        }

        // Add profile items
        int insertIndex = 0;
        foreach (var profile in profiles)
        {
            var isActive = profile.Id == activeProfile?.Id;
            var profileButton = CreateProfileButton(profile.Id, profile.Name, isActive);
            listPanel.Children.Insert(insertIndex++, profileButton);
        }
    }

    private Button CreateProfileButton(Guid profileId, string name, bool isActive)
    {
        var stack = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };

        // Color indicator
        var indicator = new Border
        {
            Width = 8,
            Height = 8,
            CornerRadius = new Avalonia.CornerRadius(4),
            Background = new SolidColorBrush(isActive ? Avalonia.Media.Color.Parse("#2d8a4e") : Avalonia.Media.Color.Parse("#4a9eff")),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        stack.Children.Add(indicator);
        
        // Profile name
        var nameText = new TextBlock
        {
            Text = name,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        nameText.Bind(TextBlock.ForegroundProperty, this.GetResourceObservable("ForegroundBrush"));
        stack.Children.Add(nameText);

        // Active label
        if (isActive)
        {
            var activeLabel = new TextBlock
            {
                Text = string.Empty,//LocalizationManager.Instance["MainWindow.Profile.Active"],
                Foreground = new SolidColorBrush(Avalonia.Media.Color.Parse("#4ecdc4")),
                FontSize = 11,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(8, 0, 0, 0)
            };
            stack.Children.Add(activeLabel);
        }

        var button = new Button
        {
            Content = stack,
            Background = Brushes.Transparent,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            Padding = new Avalonia.Thickness(8, 6),
            Tag = profileId
        };

        if (!isActive)
        {
            button.Click += async (s, e) =>
            {
                if (_profileService != null && s is Button btn && btn.Tag is Guid id)
                {
                    await _profileService.SetActiveProfileAsync(id);
                    var popup = this.FindControl<Popup>("ProfilePopup");
                    if (popup != null) popup.IsOpen = false;
                }
            };
        }

        return button;
    }

    private void ProfileDropdown_Click(object? sender, RoutedEventArgs e)
    {
        var popup = this.FindControl<Popup>("ProfilePopup");
        if (popup != null)
        {
            popup.IsOpen = !popup.IsOpen;
        }
    }

    private void Settings_Click(object? sender, RoutedEventArgs e)
    {
        var content = this.FindControl<ContentControl>("MainContent");
        if (content != null)
        {
            content.Content = new SettingsView();
        }
    }

    private void ManageProfiles_Click(object? sender, RoutedEventArgs e)
    {
        var popup = this.FindControl<Popup>("ProfilePopup");
        if (popup != null) popup.IsOpen = false;

        var content = this.FindControl<ContentControl>("MainContent");
        if (content != null)
        {
            content.Content = new SettingsView();
        }
    }

    private void ToggleSidebar_Click(object? sender, RoutedEventArgs e)
    {
        _sidebarExpanded = !_sidebarExpanded;
        UpdateSidebarState();
    }

    private void Overlay_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _sidebarExpanded = false;
        UpdateSidebarState();
    }

    private void UpdateSidebarState()
    {
        var sidebar = this.FindControl<Border>("Sidebar");
        if (sidebar != null)
        {
            sidebar.Width = _sidebarExpanded ? SidebarExpandedWidth : SidebarCollapsedWidth;
        }

        // Update toggle button position (sidebar width - button width/2)
        var toggleButton = this.FindControl<Button>("SidebarToggleButton");
        if (toggleButton != null)
        {
            double buttonMarginLeft = (_sidebarExpanded ? SidebarExpandedWidth : SidebarCollapsedWidth) - 12;
            toggleButton.Margin = new Avalonia.Thickness(buttonMarginLeft, 38, 0, 0);
        }

        // Toggle icons visibility
        var expandIcon = this.FindControl<HeroIconsAvalonia.Controls.HeroIcon>("ExpandIcon");
        var collapseIcon = this.FindControl<HeroIconsAvalonia.Controls.HeroIcon>("CollapseIcon");
        if (expandIcon != null) expandIcon.IsVisible = !_sidebarExpanded;
        if (collapseIcon != null) collapseIcon.IsVisible = _sidebarExpanded;

        // Toggle labels visibility
        var sidebarTitle = this.FindControl<TextBlock>("SidebarTitle");
        var homeLabel = this.FindControl<StackPanel>("HomeLabel");
        var encryptionLabel = this.FindControl<StackPanel>("EncryptionLabel");
        var apiKeyLabel = this.FindControl<StackPanel>("ApiKeyLabel");
        var pemLabel = this.FindControl<StackPanel>("PemLabel");
        var csrLabel = this.FindControl<StackPanel>("CsrLabel");
        var hashLabel = this.FindControl<StackPanel>("HashLabel");
        var settingsLabel = this.FindControl<StackPanel>("SettingsLabel");

        if (sidebarTitle != null) sidebarTitle.IsVisible = _sidebarExpanded;
        if (homeLabel != null) homeLabel.IsVisible = _sidebarExpanded;
        if (encryptionLabel != null) encryptionLabel.IsVisible = _sidebarExpanded;
        if (apiKeyLabel != null) apiKeyLabel.IsVisible = _sidebarExpanded;
        if (pemLabel != null) pemLabel.IsVisible = _sidebarExpanded;
        if (csrLabel != null) csrLabel.IsVisible = _sidebarExpanded;
        if (hashLabel != null) hashLabel.IsVisible = _sidebarExpanded;
        if (settingsLabel != null) settingsLabel.IsVisible = _sidebarExpanded;
    }

    private void HomeMenu_Click(object? sender, RoutedEventArgs e)
    {
        var content = this.FindControl<ContentControl>("MainContent");
        if (content != null)
        {
            content.Content = new HomeView();
        }
    }

    public void NavigateToView(string target)
    {
        var content = this.FindControl<ContentControl>("MainContent");
        if (content == null) return;

        content.Content = target switch
        {
            "PEM" => new PemView(),
            "CSR" => new CsrView(),
            "Encryption" => new EncryptionView(),
            "ApiKeys" => new ApiKeyView(),
            "Hash" => new HashView(),
            _ => new HomeView()
        };
    }

    private void PemMenu_Click(object? sender, RoutedEventArgs e)
    {
        var content = this.FindControl<ContentControl>("MainContent");
        if (content != null)
        {
            content.Content = new PemView();
        }
    }

    private void EncryptionMenu_Click(object? sender, RoutedEventArgs e)
    {
        var content = this.FindControl<ContentControl>("MainContent");
        if (content != null)
        {
            content.Content = new EncryptionView();
        }
    }

    private void ApiKeyMenu_Click(object? sender, RoutedEventArgs e)
    {
        var content = this.FindControl<ContentControl>("MainContent");
        if (content != null)
        {
            content.Content = new ApiKeyView();
        }
    }

    private void CsrMenu_Click(object? sender, RoutedEventArgs e)
    {
        var content = this.FindControl<ContentControl>("MainContent");
        if (content != null)
        {
            content.Content = new CsrView();
        }
    }

    private void HashMenu_Click(object? sender, RoutedEventArgs e)
    {
        var content = this.FindControl<ContentControl>("MainContent");
        if (content != null)
        {
            content.Content = new HashView();
        }
    }
}
