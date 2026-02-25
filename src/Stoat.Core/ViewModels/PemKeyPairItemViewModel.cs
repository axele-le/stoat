using CommunityToolkit.Mvvm.ComponentModel;

namespace Stoat.Core.ViewModels;

/// <summary>
/// ViewModel for a single PEM key pair item in the history list.
/// </summary>
public partial class PemKeyPairItemViewModel : ObservableObject
{
    /// <summary>
    /// Unique identifier for the key pair.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Human-readable name for the key pair.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Algorithm identifier (e.g., "RSA-2048").
    /// </summary>
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>
    /// The public key in PEM format.
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the key pair was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Whether this key pair has a private key.
    /// </summary>
    public bool HasPrivateKey { get; set; }

    /// <summary>
    /// Masked display string for the key content.
    /// </summary>
    public static string MaskedKey => new string('\u2022', 40);

    /// <summary>
    /// Whether to show the private key content.
    /// </summary>
    [ObservableProperty]
    private bool _showPrivateKey;

    /// <summary>
    /// Whether to show the public key content.
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
    /// Gets the display text for the private key based on visibility.
    /// </summary>
    public string PrivateKeyDisplay => ShowPrivateKey && !string.IsNullOrEmpty(PrivateKey)
        ? PrivateKey
        : MaskedKey;

    /// <summary>
    /// Gets the display text for the public key based on visibility.
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
