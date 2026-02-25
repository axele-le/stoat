namespace Stoat.Services.Interfaces;

public interface IConfirmationService
{
    Task<bool> ConfirmAsync(string title, string message);
}
