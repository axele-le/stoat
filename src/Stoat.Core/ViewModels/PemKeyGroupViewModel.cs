using CommunityToolkit.Mvvm.ComponentModel;

namespace Stoat.Core.ViewModels;

/// <summary>
/// ViewModel for a PEM key pair group in the table view.
/// Each group represents a single key pair with private and public keys.
/// </summary>
public partial class PemKeyGroupViewModel : ObservableObject
{
    /// <summary>
    /// Reference to parent ViewModel for command binding.
    /// </summary>
    public PemViewModel? ParentViewModel { get; set; }

    /// <summary>
    /// Unique identifier for the key pair.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Human-readable name for the key pair.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Algorithm identifier (e.g., "RSA-2048", "RSA-4096").
    /// </summary>
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the key pair was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Whether this key pair has a private key.
    /// </summary>
    public bool HasPrivateKey { get; set; }

    /// <summary>
    /// The public key in PEM format.
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Masked display string for the key content.
    /// </summary>
    public static string MaskedKey => new string('\u2022', 40);

    /// <summary>
    /// Whether the private key header panel is expanded.
    /// </summary>
    [ObservableProperty]
    private bool _showPrivateKeyPanel;

    /// <summary>
    /// Whether the private key content in the table row is visible.
    /// </summary>
    [ObservableProperty]
    private bool _showPrivateKey;

    /// <summary>
    /// Whether the public key content in the table row is visible.
    /// </summary>
    [ObservableProperty]
    private bool _showPublicKey;

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
    /// Whether the private key has been copied recently (for feedback).
    /// </summary>
    [ObservableProperty]
    private bool _isPrivateKeyCopied;

    /// <summary>
    /// Whether the public key has been copied recently (for feedback).
    /// </summary>
    [ObservableProperty]
    private bool _isPublicKeyCopied;

    /// <summary>
    /// Display format for the creation date.
    /// </summary>
    public string CreatedDateDisplay => CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy");

    /// <summary>
    /// Gets the display text for the private key in the table row based on visibility.
    /// </summary>
    public string PrivateKeyDisplay => ShowPrivateKey && !string.IsNullOrEmpty(PrivateKey)
        ? PrivateKey
        : MaskedKey;

    /// <summary>
    /// Gets the display text for the public key in the table row based on visibility.
    /// </summary>
    public string PublicKeyDisplay => ShowPublicKey
        ? PublicKey
        : MaskedKey;

    partial void OnShowPrivateKeyChanged(bool value)
    {
        OnPropertyChanged(nameof(PrivateKeyDisplay));
    }

    partial void OnShowPublicKeyChanged(bool value)
    {
        OnPropertyChanged(nameof(PublicKeyDisplay));
    }

    partial void OnPrivateKeyChanged(string? value)
    {
        OnPropertyChanged(nameof(PrivateKeyDisplay));
    }
}
