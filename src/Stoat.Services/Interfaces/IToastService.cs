using Stoat.Services.Enums;

namespace Stoat.Services.Interfaces;

public interface IToastService
{
    void Show(string message, ToastType type = ToastType.Info, int? durationMs = null);
    void Success(string message, int? durationMs = null);
    void Error(string message, int? durationMs = null);
    void Warning(string message, int? durationMs = null);
    void Info(string message, int? durationMs = null);
    void Dismiss(Guid toastId);
    void DismissAll();
}
