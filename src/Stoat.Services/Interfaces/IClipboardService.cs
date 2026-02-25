namespace Stoat.Services.Interfaces;

/// <summary>
/// Service for clipboard operations with automatic timed clearing of sensitive data.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Copies text to the clipboard. If auto-clear is enabled, starts/resets the clear timer.
    /// </summary>
    Task CopyAsync(string text);

    /// <summary>
    /// Clears the clipboard immediately and stops any pending timer.
    /// </summary>
    Task ClearAsync();

    /// <summary>
    /// Updates the auto-clear settings (enabled state and timeout).
    /// </summary>
    void UpdateSettings(bool enabled, int timeoutSeconds);

    /// <summary>
    /// Raised when the clipboard has been automatically cleared by the timer.
    /// </summary>
    event EventHandler? ClipboardCleared;
}
