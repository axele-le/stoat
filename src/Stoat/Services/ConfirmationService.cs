using System.ComponentModel;
using Stoat.Services.Interfaces;

namespace Stoat.Services;

/// <summary>
/// Confirmation dialog service. Lives in UI layer because it drives a visual overlay.
/// When ConfirmOperationsEnabled is false, all confirmations are auto-approved.
/// </summary>
public class ConfirmationService : IConfirmationService, INotifyPropertyChanged
{
    private readonly IClientSettingsService _clientSettingsService;
    private TaskCompletionSource<bool>? _tcs;

    private bool _isVisible;
    private string _title = string.Empty;
    private string _message = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsVisible
    {
        get => _isVisible;
        private set
        {
            if (_isVisible == value) return;
            _isVisible = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
        }
    }

    public string Title
    {
        get => _title;
        private set
        {
            if (_title == value) return;
            _title = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
        }
    }

    public string Message
    {
        get => _message;
        private set
        {
            if (_message == value) return;
            _message = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Message)));
        }
    }

    public ConfirmationService(IClientSettingsService clientSettingsService)
    {
        _clientSettingsService = clientSettingsService;
    }

    public async Task<bool> ConfirmAsync(string title, string message)
    {
        var settings = await _clientSettingsService.LoadAsync();
        if (!settings.ConfirmOperationsEnabled)
            return true;

        // Ensure we're on the UI thread
        if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            return await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => ConfirmAsync(title, message));
        }

        _tcs = new TaskCompletionSource<bool>();
        Title = title;
        Message = message;
        IsVisible = true;

        var result = await _tcs.Task;

        IsVisible = false;
        Title = string.Empty;
        Message = string.Empty;
        _tcs = null;

        return result;
    }

    public void OnConfirmed()
    {
        _tcs?.TrySetResult(true);
    }

    public void OnCancelled()
    {
        _tcs?.TrySetResult(false);
    }
}
