using System.ComponentModel;
using System.Windows.Input;
using Stoat.Services.Enums;

namespace Stoat.Services.Models;

public class ToastItem : INotifyPropertyChanged
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Message { get; init; } = string.Empty;
    public ToastType Type { get; init; }
    public int DurationMs { get; init; }
    public ICommand? DismissCommand { get; set; }

    private bool _isRemoving;
    public bool IsRemoving
    {
        get => _isRemoving;
        set
        {
            if (_isRemoving == value) return;
            _isRemoving = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRemoving)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
