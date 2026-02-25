namespace Stoat.Services.Models;

/// <summary>
/// Persistent client settings stored as DPAPI-protected file.
/// </summary>
public class ClientSettings
{
    // Sicurezza
    public bool AutoLockEnabled { get; set; }
    public int LockTimeoutIndex { get; set; } = 1; // 0=1min, 1=5min, 2=15min, 3=30min
    public bool ClearClipboardEnabled { get; set; } = true;
    public int ClipboardTimeoutIndex { get; set; } = 1; // 0=10s, 1=30s, 2=60s, 3=120s
    public bool ConfirmOperationsEnabled { get; set; } = true;

    // Aspetto
    public int SelectedThemeIndex { get; set; } // 0=Scuro, 1=Chiaro, 2=Sistema
    public int SelectedLanguageIndex { get; set; } // 0=Italiano, 1=English, 2=Deutsch, 3=Français, 4=Español, 5=Polski

    /// <summary>
    /// Returns the lock timeout in minutes based on LockTimeoutIndex.
    /// </summary>
    public int GetLockTimeoutMinutes() => LockTimeoutIndex switch
    {
        0 => 1,
        1 => 5,
        2 => 15,
        3 => 30,
        _ => 5
    };

    /// <summary>
    /// Returns the clipboard clear timeout in seconds based on ClipboardTimeoutIndex.
    /// </summary>
    public int GetClipboardTimeoutSeconds() => ClipboardTimeoutIndex switch
    {
        0 => 10,
        1 => 30,
        2 => 60,
        3 => 120,
        _ => 30
    };
}
