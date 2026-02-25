using Avalonia.Input.Platform;
using Stoat.Services.Interfaces;

namespace Stoat.Services;

/// <summary>
/// Clipboard service with automatic timed clearing of sensitive data.
/// Lives in UI layer because it needs access to Avalonia's IClipboard.
/// </summary>
public class ClipboardService : IClipboardService, IDisposable
{
    private Func<IClipboard?>? _clipboardProvider;
    private readonly object _lock = new();
    private Timer? _clearTimer;
    private bool _enabled;
    private int _timeoutSeconds;

    public event EventHandler? ClipboardCleared;

    /// <summary>
    /// Sets the clipboard provider. Called after MainWindow is created
    /// since IClipboard requires a TopLevel.
    /// </summary>
    public void UpdateClipboardProvider(Func<IClipboard?> provider)
    {
        _clipboardProvider = provider;
    }

    public async Task CopyAsync(string text)
    {
        var clipboard = _clipboardProvider?.Invoke();
        if (clipboard == null) return;

        await clipboard.SetTextAsync(text);

        lock (_lock)
        {
            _clearTimer?.Dispose();
            _clearTimer = null;

            if (_enabled && _timeoutSeconds > 0)
            {
                _clearTimer = new Timer(OnTimerElapsed, null, _timeoutSeconds * 1000, Timeout.Infinite);
            }
        }
    }

    public async Task ClearAsync()
    {
        lock (_lock)
        {
            _clearTimer?.Dispose();
            _clearTimer = null;
        }

        var clipboard = _clipboardProvider?.Invoke();
        if (clipboard == null) return;

        await clipboard.SetTextAsync(string.Empty);
    }

    public void UpdateSettings(bool enabled, int timeoutSeconds)
    {
        lock (_lock)
        {
            _enabled = enabled;
            _timeoutSeconds = timeoutSeconds;

            if (!enabled)
            {
                _clearTimer?.Dispose();
                _clearTimer = null;
            }
        }
    }

    private void OnTimerElapsed(object? state)
    {
        lock (_lock)
        {
            _clearTimer?.Dispose();
            _clearTimer = null;
        }

        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            var clipboard = _clipboardProvider?.Invoke();
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(string.Empty);
            }

            ClipboardCleared?.Invoke(this, EventArgs.Empty);
        });
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _clearTimer?.Dispose();
            _clearTimer = null;
        }
    }
}
