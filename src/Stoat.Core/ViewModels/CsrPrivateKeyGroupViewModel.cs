using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Stoat.Core.ViewModels;

/// <summary>
/// ViewModel for a group of CSRs sharing the same private key.
/// Each group displays a private key panel with a DataGrid of CSRs.
/// </summary>
public partial class CsrPrivateKeyGroupViewModel : ObservableObject
{
    /// <summary>
    /// Reference to parent ViewModel for command binding.
    /// </summary>
    public CsrViewModel? ParentViewModel { get; set; }

    /// <summary>
    /// Unique identifier for the group (derived from public key hash).
    /// </summary>
    public string GroupId { get; set; } = string.Empty;

    /// <summary>
    /// Display number for the group (e.g., 1 for "Chiave Privata #1").
    /// </summary>
    public int GroupNumber { get; set; }

    /// <summary>
    /// Custom name for the group. If empty, displays "Chiave Privata #N".
    /// </summary>
    [ObservableProperty]
    private string? _groupName;

    /// <summary>
    /// Display name for the group header.
    /// Returns GroupName if set, otherwise "Chiave Privata #N".
    /// </summary>
    public string DisplayName => !string.IsNullOrWhiteSpace(GroupName)
        ? GroupName
        : $"Chiave Privata #{GroupNumber}";

    /// <summary>
    /// Whether the group is in edit mode for renaming.
    /// </summary>
    [ObservableProperty]
    private bool _isEditingName;

    /// <summary>
    /// Algorithm type for the key (e.g., "RSA-2048", "ECDSA-P256").
    /// </summary>
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>
    /// The first CSR ID in this group (used for loading private key).
    /// </summary>
    public Guid FirstCsrId { get; set; }

    /// <summary>
    /// CSRs in this group.
    /// </summary>
    public ObservableCollection<CsrItemViewModel> Csrs { get; } = new();

    /// <summary>
    /// Number of CSRs in the group.
    /// </summary>
    public int CsrCount => Csrs.Count;

    /// <summary>
    /// Display text for CSR count (e.g., "3 CSR").
    /// </summary>
    public string CsrCountDisplay => $"{CsrCount} CSR";

    /// <summary>
    /// Whether the private key content is visible.
    /// </summary>
    [ObservableProperty]
    private bool _showPrivateKey;

    /// <summary>
    /// The decrypted private key content (loaded on demand).
    /// </summary>
    [ObservableProperty]
    private string? _privateKey;

    /// <summary>
    /// Whether the private key is currently being loaded.
    /// </summary>
    [ObservableProperty]
    private bool _isLoadingPrivateKey;

    /// <summary>
    /// Whether this group panel is expanded.
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded = true;

    /// <summary>
    /// Whether any CSR in this group has a private key.
    /// </summary>
    public bool HasPrivateKey => Csrs.Any(c => c.HasPrivateKey);

    /// <summary>
    /// Whether the key has been copied recently (for feedback).
    /// </summary>
    [ObservableProperty]
    private bool _isCopied;

    /// <summary>
    /// Updates the CsrCount property when Csrs collection changes.
    /// </summary>
    public void RefreshCsrCount()
    {
        OnPropertyChanged(nameof(CsrCount));
        OnPropertyChanged(nameof(CsrCountDisplay));
        OnPropertyChanged(nameof(HasPrivateKey));
    }

    partial void OnGroupNameChanged(string? value)
    {
        OnPropertyChanged(nameof(DisplayName));
    }
}
