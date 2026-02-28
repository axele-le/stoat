using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stoat.Services.Enums;
using Stoat.Services.Models;

namespace Stoat.Controls;

public class AnimatedToast : Border
{
    private bool _isAnimating;
    private bool _attached;
    private DispatcherTimer? _progressTimer;
    private Stopwatch? _progressStopwatch;
    private ToastItem? _currentToast;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;

        SubscribeToToast();
        SetTypeClass();
        StartEntranceAnimation();
        Dispatcher.UIThread.Post(StartProgressAnimation, DispatcherPriority.Loaded);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
        StopProgressTimer();
        UnsubscribeFromToast();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        UnsubscribeFromToast();
        SubscribeToToast();

        if (_attached)
        {
            SetTypeClass();
            StartEntranceAnimation();
            Dispatcher.UIThread.Post(StartProgressAnimation, DispatcherPriority.Loaded);
        }
    }

    private void SubscribeToToast()
    {
        if (DataContext is ToastItem toast)
        {
            _currentToast = toast;
            toast.PropertyChanged += OnToastPropertyChanged;
        }
    }

    private void UnsubscribeFromToast()
    {
        if (_currentToast != null)
        {
            _currentToast.PropertyChanged -= OnToastPropertyChanged;
            _currentToast = null;
        }
    }

    private void OnToastPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ToastItem.IsRemoving) && _currentToast is { IsRemoving: true })
        {
            StopProgressTimer();

            if (Dispatcher.UIThread.CheckAccess())
                StartExitAnimation();
            else
                Dispatcher.UIThread.Post(StartExitAnimation);
        }
    }

    private void SetTypeClass()
    {
        if (DataContext is not ToastItem toast) return;

        Classes.Remove("success");
        Classes.Remove("error");
        Classes.Remove("warning");
        Classes.Remove("info");

        var className = toast.Type switch
        {
            ToastType.Success => "success",
            ToastType.Error => "error",
            ToastType.Warning => "warning",
            ToastType.Info => "info",
            _ => "info"
        };
        Classes.Add(className);
    }

    private void StartEntranceAnimation()
    {
        if (_isAnimating) return;
        _isAnimating = true;

        Opacity = 0;
        RenderTransform = new TranslateTransform(0, -35);

        Dispatcher.UIThread.Post(() =>
        {
            if (_attached && this.GetVisualRoot() != null)
            {
                Opacity = 1;
                RenderTransform = new TranslateTransform(0, 0);
            }
            _isAnimating = false;
        }, DispatcherPriority.Loaded);
    }

    private void StartExitAnimation()
    {
        Opacity = 0;
        RenderTransform = new TranslateTransform(0, -20);
    }

    private void StartProgressAnimation()
    {
        StopProgressTimer();

        if (DataContext is not ToastItem toast || toast.DurationMs <= 0) return;
        if (!_attached) return;

        var progressBar = this.GetVisualDescendants()
            .OfType<Border>()
            .FirstOrDefault(b => b.Classes.Contains("progressBar"));

        if (progressBar?.RenderTransform is not ScaleTransform scaleTransform) return;

        scaleTransform.ScaleX = 1;
        var durationMs = (double)toast.DurationMs;

        _progressStopwatch = Stopwatch.StartNew();
        _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _progressTimer.Tick += (_, _) =>
        {
            var elapsed = _progressStopwatch.Elapsed.TotalMilliseconds;
            var progress = Math.Max(0, 1.0 - elapsed / durationMs);
            scaleTransform.ScaleX = progress;

            if (progress <= 0)
            {
                StopProgressTimer();
            }
        };
        _progressTimer.Start();
    }

    private void StopProgressTimer()
    {
        _progressTimer?.Stop();
        _progressTimer = null;
        _progressStopwatch?.Stop();
        _progressStopwatch = null;
    }
}
