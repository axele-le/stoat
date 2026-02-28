using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Stoat.Services.Enums;
using Stoat.Services.Interfaces;
using Stoat.Services.Models;

namespace Stoat.Services;

public class ToastService : IToastService, IDisposable
{
    private const int MaxToasts = 5;
    private const int RemoveAnimationMs = 250;

    private readonly Dictionary<Guid, System.Threading.Timer> _timers = new();
    private readonly object _lock = new();
    private bool _disposed;

    public ObservableCollection<ToastItem> Toasts { get; } = new();

    public void Show(string message, ToastType type = ToastType.Info, int? durationMs = null)
    {
        var duration = durationMs ?? GetDefaultDuration(type);

        var toast = new ToastItem
        {
            Message = message,
            Type = type,
            DurationMs = duration
        };
        toast.DismissCommand = new RelayCommand(() => Dismiss(toast.Id));

        if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => AddToast(toast));
        }
        else
        {
            AddToast(toast);
        }
    }

    public void Success(string message, int? durationMs = null)
        => Show(message, ToastType.Success, durationMs);

    public void Error(string message, int? durationMs = null)
        => Show(message, ToastType.Error, durationMs);

    public void Warning(string message, int? durationMs = null)
        => Show(message, ToastType.Warning, durationMs);

    public void Info(string message, int? durationMs = null)
        => Show(message, ToastType.Info, durationMs);

    public void Dismiss(Guid toastId)
    {
        if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => Dismiss(toastId));
            return;
        }

        var toast = FindToast(toastId);
        if (toast == null || toast.IsRemoving) return;

        toast.IsRemoving = true;
        StopTimer(toastId);

        // Waiting animation before removing from collection
        var removeTimer = new System.Threading.Timer(_ =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => RemoveToast(toastId));
        }, null, RemoveAnimationMs, System.Threading.Timeout.Infinite);

        lock (_lock)
        {
            _timers[toastId] = removeTimer;
        }
    }

    public void DismissAll()
    {
        if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(DismissAll);
            return;
        }

        var ids = Toasts.Select(t => t.Id).ToList();
        foreach (var id in ids)
        {
            Dismiss(id);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            foreach (var timer in _timers.Values)
            {
                timer.Dispose();
            }
            _timers.Clear();
        }

        GC.SuppressFinalize(this);
    }

    private void AddToast(ToastItem toast)
    {
        while (Toasts.Count >= MaxToasts)
        {
            var oldest = Toasts[0];
            StopTimer(oldest.Id);
            Toasts.RemoveAt(0);
        }

        Toasts.Add(toast);

        if (toast.DurationMs > 0)
        {
            var timer = new System.Threading.Timer(_ =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => Dismiss(toast.Id));
            }, null, toast.DurationMs, System.Threading.Timeout.Infinite);

            lock (_lock)
            {
                _timers[toast.Id] = timer;
            }
        }
    }

    private void RemoveToast(Guid toastId)
    {
        var toast = FindToast(toastId);
        if (toast != null)
        {
            Toasts.Remove(toast);
        }
        StopTimer(toastId);
    }

    private ToastItem? FindToast(Guid toastId)
    {
        return Toasts.FirstOrDefault(t => t.Id == toastId);
    }

    private void StopTimer(Guid toastId)
    {
        lock (_lock)
        {
            if (_timers.Remove(toastId, out var timer))
            {
                timer.Dispose();
            }
        }
    }

    private static int GetDefaultDuration(ToastType type) => type switch
    {
        ToastType.Success => 3000,
        ToastType.Error => 5000,
        ToastType.Warning => 4000,
        ToastType.Info => 3500,
        _ => 3500
    };
}
