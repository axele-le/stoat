using CommunityToolkit.Mvvm.ComponentModel;
using Stoat.Services.Enums;

namespace Stoat.Core.ViewModels;

/// <summary>
/// ViewModel for a Subject Alternative Name entry in the CSR form.
/// </summary>
public partial class SanEntryViewModel : ObservableObject
{
    /// <summary>
    /// The type of SAN entry (DNS, IP, Email, URI).
    /// </summary>
    [ObservableProperty]
    private SanType _type = SanType.DNS;

    /// <summary>
    /// The value of the SAN entry.
    /// </summary>
    [ObservableProperty]
    private string _value = string.Empty;

    /// <summary>
    /// Available SAN types for selection.
    /// </summary>
    public static SanType[] AvailableSanTypes => Enum.GetValues<SanType>();

    /// <summary>
    /// Gets the placeholder text based on the selected type.
    /// </summary>
    public string Placeholder => Type switch
    {
        SanType.DNS => "es. www.example.com, *.example.com",
        SanType.IP => "es. 192.168.1.1, 2001:db8::1",
        SanType.Email => "es. admin@example.com",
        SanType.URI => "es. https://example.com",
        _ => ""
    };

    partial void OnTypeChanged(SanType value)
    {
        OnPropertyChanged(nameof(Placeholder));
    }
}
