using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Stoat.Services.Enums;
using Stoat.Services.Interfaces;
using Stoat.Services.Models;
using Stoat.Services.Utils;

namespace Stoat.Core.ViewModels;

/// <summary>
/// ViewModel for the CSR creation form.
/// </summary>
public partial class CsrFormViewModel : ObservableObject
{
    private readonly ICsrService _csrService;
    private readonly ICsrStorageService _csrStorageService;
    private readonly ILocalizationService _localizationService;

    // Key source selection
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UseNewKey))]
    [NotifyPropertyChangedFor(nameof(ShowNewKeyOptions))]
    [NotifyPropertyChangedFor(nameof(ShowExistingKeySelector))]
    [NotifyCanExecuteChangedFor(nameof(GenerateCsrCommand))]
    private bool _useExistingKey;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateCsrCommand))]
    private ExistingKeyInfo? _selectedExistingKey;

    /// <summary>
    /// Available existing keys that can be reused.
    /// </summary>
    public ObservableCollection<ExistingKeyInfo> ExistingKeys { get; } = new();

    /// <summary>
    /// Whether there are existing keys available.
    /// </summary>
    public bool HasExistingKeys => ExistingKeys.Count > 0;

    /// <summary>
    /// Whether to use a new key (inverse of UseExistingKey).
    /// </summary>
    public bool UseNewKey => !UseExistingKey;

    /// <summary>
    /// Whether to show the new key algorithm options.
    /// </summary>
    public bool ShowNewKeyOptions => !UseExistingKey;

    /// <summary>
    /// Whether to show the existing key selector.
    /// </summary>
    public bool ShowExistingKeySelector => UseExistingKey;

    // Preset
    [ObservableProperty]
    private CsrPresetType? _selectedPreset;

    // Identificativo
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateCsrCommand))]
    private string _name = string.Empty;

    // Subject DN
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateCsrCommand))]
    private string _commonName = string.Empty;

    [ObservableProperty]
    private string _organization = string.Empty;

    [ObservableProperty]
    private string _organizationalUnit = string.Empty;

    [ObservableProperty]
    private string? _selectedCountry;

    [ObservableProperty]
    private string _state = string.Empty;

    [ObservableProperty]
    private string _locality = string.Empty;

    [ObservableProperty]
    private string _emailAddress = string.Empty;

    // SAN
    public ObservableCollection<SanEntryViewModel> SanEntries { get; } = new();

    // Key Algorithm
    [ObservableProperty]
    private CsrKeyAlgorithm _selectedKeyAlgorithm = CsrKeyAlgorithm.RSA_2048;

    [ObservableProperty]
    private CsrSignatureAlgorithm _selectedSignatureAlgorithm = CsrSignatureAlgorithm.SHA256WithRSA;

    [ObservableProperty]
    private IReadOnlyList<CsrSignatureAlgorithm> _availableSignatureAlgorithms = Array.Empty<CsrSignatureAlgorithm>();

    // Advanced Options
    [ObservableProperty]
    private bool _showAdvancedOptions;

    [ObservableProperty]
    private bool _keyUsageDigitalSignature;

    [ObservableProperty]
    private bool _keyUsageKeyEncipherment;

    [ObservableProperty]
    private bool _keyUsageDataEncipherment;

    [ObservableProperty]
    private bool _keyUsageKeyAgreement;

    [ObservableProperty]
    private bool _keyUsageNonRepudiation;

    [ObservableProperty]
    private bool _keyUsageKeyCertSign;

    [ObservableProperty]
    private bool _keyUsageCrlSign;

    [ObservableProperty]
    private bool _ekuServerAuth;

    [ObservableProperty]
    private bool _ekuClientAuth;

    [ObservableProperty]
    private bool _ekuCodeSigning;

    [ObservableProperty]
    private bool _ekuEmailProtection;

    [ObservableProperty]
    private bool _ekuTimeStamping;

    [ObservableProperty]
    private bool _isCA;

    // Status
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateCsrCommand))]
    private bool _isGenerating;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isError;

    // Available options
    public CsrKeyAlgorithm[] AvailableKeyAlgorithms { get; } = Enum.GetValues<CsrKeyAlgorithm>();
    public CsrPresetType[] AvailablePresets { get; } = Enum.GetValues<CsrPresetType>();
    public IReadOnlyList<CountryInfo> Countries { get; } = CountryHelper.GetCountries();

    /// <summary>
    /// Event raised when the form should be closed.
    /// </summary>
    public event Action? CloseRequested;

    /// <summary>
    /// Event raised when CSR generation is successful.
    /// </summary>
    public event Action? GenerationSucceeded;

    public CsrFormViewModel(ICsrService csrService, ICsrStorageService csrStorageService, ILocalizationService localizationService)
    {
        _csrService = csrService;
        _csrStorageService = csrStorageService;
        _localizationService = localizationService;

        // Initialize with compatible signature algorithms
        UpdateCompatibleSignatureAlgorithms();
    }

    partial void OnSelectedKeyAlgorithmChanged(CsrKeyAlgorithm value)
    {
        UpdateCompatibleSignatureAlgorithms();
    }

    partial void OnUseExistingKeyChanged(bool value)
    {
        if (!value)
        {
            SelectedExistingKey = null;
        }
        UpdateCompatibleSignatureAlgorithms();
    }

    partial void OnSelectedExistingKeyChanged(ExistingKeyInfo? value)
    {
        UpdateCompatibleSignatureAlgorithms();
    }

    /// <summary>
    /// Gets the effective key algorithm based on selection.
    /// </summary>
    private CsrKeyAlgorithm EffectiveKeyAlgorithm =>
        UseExistingKey && SelectedExistingKey != null
            ? SelectedExistingKey.KeyAlgorithm
            : SelectedKeyAlgorithm;

    private void UpdateCompatibleSignatureAlgorithms()
    {
        AvailableSignatureAlgorithms = _csrService.GetCompatibleSignatureAlgorithms(EffectiveKeyAlgorithm);
        if (AvailableSignatureAlgorithms.Count > 0 && !AvailableSignatureAlgorithms.Contains(SelectedSignatureAlgorithm))
        {
            SelectedSignatureAlgorithm = AvailableSignatureAlgorithms[0];
        }
    }

    [RelayCommand]
    private void SetUseExistingKey(bool value)
    {
        UseExistingKey = value;
    }

    [RelayCommand]
    private void ApplyPreset(CsrPresetType preset)
    {
        SelectedPreset = preset;
        var defaults = _csrService.GetPresetDefaults(preset);

        SelectedKeyAlgorithm = defaults.KeyAlgorithm;
        SelectedSignatureAlgorithm = defaults.SignatureAlgorithm;

        // Key Usage
        KeyUsageDigitalSignature = (defaults.KeyUsage & KeyUsageFlags.DigitalSignature) != 0;
        KeyUsageKeyEncipherment = (defaults.KeyUsage & KeyUsageFlags.KeyEncipherment) != 0;
        KeyUsageDataEncipherment = (defaults.KeyUsage & KeyUsageFlags.DataEncipherment) != 0;
        KeyUsageKeyAgreement = (defaults.KeyUsage & KeyUsageFlags.KeyAgreement) != 0;
        KeyUsageNonRepudiation = (defaults.KeyUsage & KeyUsageFlags.NonRepudiation) != 0;
        KeyUsageKeyCertSign = (defaults.KeyUsage & KeyUsageFlags.KeyCertSign) != 0;
        KeyUsageCrlSign = (defaults.KeyUsage & KeyUsageFlags.CrlSign) != 0;

        // Extended Key Usage
        EkuServerAuth = defaults.ExtendedKeyUsage.Contains(ExtendedKeyUsageOid.ServerAuth);
        EkuClientAuth = defaults.ExtendedKeyUsage.Contains(ExtendedKeyUsageOid.ClientAuth);
        EkuCodeSigning = defaults.ExtendedKeyUsage.Contains(ExtendedKeyUsageOid.CodeSigning);
        EkuEmailProtection = defaults.ExtendedKeyUsage.Contains(ExtendedKeyUsageOid.EmailProtection);
        EkuTimeStamping = defaults.ExtendedKeyUsage.Contains(ExtendedKeyUsageOid.TimeStamping);

        IsCA = defaults.IsCA;

        SetStatus(_localizationService?.Format("Csr.Form.Success.PresetApplied", GetPresetDisplayName(preset)) ?? string.Empty, isError: false);
    }

    [RelayCommand]
    private void AddSanEntry()
    {
        SanEntries.Add(new SanEntryViewModel());
    }

    [RelayCommand]
    private void RemoveSanEntry(SanEntryViewModel entry)
    {
        SanEntries.Remove(entry);
    }

    [RelayCommand(CanExecute = nameof(CanGenerateCsr))]
    private async Task GenerateCsrAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            SetStatus(_localizationService?.Format("Csr.Form.Error.NoIdentifier") ?? string.Empty, isError: true);
            return;
        }

        if (string.IsNullOrWhiteSpace(CommonName))
        {
            SetStatus(_localizationService?.Format("Csr.Form.Error.NoCN") ?? string.Empty, isError: true);
            return;
        }

        if (UseExistingKey && SelectedExistingKey == null)
        {
            SetStatus(_localizationService?.Format("Csr.Form.Error.NoExistingKey") ?? string.Empty, isError: true);
            return;
        }

        IsGenerating = true;
        IsError = false;
        StatusMessage = _localizationService?.Format("Csr.Form.Status.Generating") ?? string.Empty;

        try
        {
            var csrData = BuildCsrData();
            string csrPem, privateKeyPem, publicKeyPem;

            if (UseExistingKey && SelectedExistingKey != null)
            {
                // Get the existing private key
                var existingPrivateKey = await _csrStorageService.GetDecryptedPrivateKeyAsync(SelectedExistingKey.CsrId);

                // Generate CSR with existing key
                var result = await Task.Run(() => _csrService.GenerateCsrWithExistingKey(csrData, existingPrivateKey));
                csrPem = result.CsrPem;
                privateKeyPem = existingPrivateKey;
                publicKeyPem = result.PublicKeyPem;
            }
            else
            {
                // Generate CSR with new key pair
                var result = await Task.Run(() => _csrService.GenerateCsr(csrData));
                csrPem = result.CsrPem;
                privateKeyPem = result.PrivateKeyPem;
                publicKeyPem = result.PublicKeyPem;
            }

            // Save to storage
            await _csrStorageService.SaveCsrAsync(
                Name.Trim(),
                csrPem,
                privateKeyPem,
                publicKeyPem,
                csrData);

            SetStatus(_localizationService?.Format("Csr.Form.Success.Generated") ?? string.Empty, isError: false);
            GenerationSucceeded?.Invoke();

            // Clear form
            ClearForm();
        }
        catch (Exception ex)
        {
            SetStatus(_localizationService?.Format("Common.Error.WithMessage", ex.Message) ?? string.Empty, isError: true);
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private bool CanGenerateCsr()
    {
        if (IsGenerating) return false;
        if (string.IsNullOrWhiteSpace(Name)) return false;
        if (string.IsNullOrWhiteSpace(CommonName)) return false;
        if (UseExistingKey && SelectedExistingKey == null) return false;
        return true;
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke();
    }

    private CsrData BuildCsrData()
    {
        var data = new CsrData
        {
            CommonName = CommonName.Trim(),
            Organization = string.IsNullOrWhiteSpace(Organization) ? null : Organization.Trim(),
            OrganizationalUnit = string.IsNullOrWhiteSpace(OrganizationalUnit) ? null : OrganizationalUnit.Trim(),
            Country = SelectedCountry,
            State = string.IsNullOrWhiteSpace(State) ? null : State.Trim(),
            Locality = string.IsNullOrWhiteSpace(Locality) ? null : Locality.Trim(),
            EmailAddress = string.IsNullOrWhiteSpace(EmailAddress) ? null : EmailAddress.Trim(),
            KeyAlgorithm = SelectedKeyAlgorithm,
            SignatureAlgorithm = SelectedSignatureAlgorithm,
            IsCA = IsCA
        };

        // Key Usage
        var keyUsage = KeyUsageFlags.None;
        if (KeyUsageDigitalSignature) keyUsage |= KeyUsageFlags.DigitalSignature;
        if (KeyUsageKeyEncipherment) keyUsage |= KeyUsageFlags.KeyEncipherment;
        if (KeyUsageDataEncipherment) keyUsage |= KeyUsageFlags.DataEncipherment;
        if (KeyUsageKeyAgreement) keyUsage |= KeyUsageFlags.KeyAgreement;
        if (KeyUsageNonRepudiation) keyUsage |= KeyUsageFlags.NonRepudiation;
        if (KeyUsageKeyCertSign) keyUsage |= KeyUsageFlags.KeyCertSign;
        if (KeyUsageCrlSign) keyUsage |= KeyUsageFlags.CrlSign;
        data.KeyUsage = keyUsage;

        // Extended Key Usage
        var eku = new List<ExtendedKeyUsageOid>();
        if (EkuServerAuth) eku.Add(ExtendedKeyUsageOid.ServerAuth);
        if (EkuClientAuth) eku.Add(ExtendedKeyUsageOid.ClientAuth);
        if (EkuCodeSigning) eku.Add(ExtendedKeyUsageOid.CodeSigning);
        if (EkuEmailProtection) eku.Add(ExtendedKeyUsageOid.EmailProtection);
        if (EkuTimeStamping) eku.Add(ExtendedKeyUsageOid.TimeStamping);
        data.ExtendedKeyUsage = eku;

        // SANs
        foreach (var san in SanEntries)
        {
            if (!string.IsNullOrWhiteSpace(san.Value))
            {
                data.SubjectAlternativeNames.Add(new SanEntry
                {
                    Type = san.Type,
                    Value = san.Value.Trim()
                });
            }
        }

        return data;
    }

    private void ClearForm()
    {
        Name = string.Empty;
        CommonName = string.Empty;
        Organization = string.Empty;
        OrganizationalUnit = string.Empty;
        SelectedCountry = null;
        State = string.Empty;
        Locality = string.Empty;
        EmailAddress = string.Empty;
        SanEntries.Clear();
        SelectedPreset = null;
        UseExistingKey = false;
        SelectedExistingKey = null;
        SelectedKeyAlgorithm = CsrKeyAlgorithm.RSA_2048;
        SelectedSignatureAlgorithm = CsrSignatureAlgorithm.SHA256WithRSA;
        ShowAdvancedOptions = false;
        KeyUsageDigitalSignature = false;
        KeyUsageKeyEncipherment = false;
        KeyUsageDataEncipherment = false;
        KeyUsageKeyAgreement = false;
        KeyUsageNonRepudiation = false;
        KeyUsageKeyCertSign = false;
        KeyUsageCrlSign = false;
        EkuServerAuth = false;
        EkuClientAuth = false;
        EkuCodeSigning = false;
        EkuEmailProtection = false;
        EkuTimeStamping = false;
        IsCA = false;
    }

    /// <summary>
    /// Sets the available existing keys that can be reused.
    /// </summary>
    /// <param name="keys">The list of existing keys.</param>
    public void SetExistingKeys(IEnumerable<ExistingKeyInfo> keys)
    {
        ExistingKeys.Clear();
        foreach (var key in keys)
        {
            ExistingKeys.Add(key);
        }
        OnPropertyChanged(nameof(HasExistingKeys));
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsError = isError;
    }

    private static string GetPresetDisplayName(CsrPresetType preset)
    {
        return preset switch
        {
            CsrPresetType.WebServer => "Web Server",
            CsrPresetType.ClientAuth => "Client Auth",
            CsrPresetType.CodeSigning => "Code Signing",
            CsrPresetType.Email => "Email/S-MIME",
            _ => preset.ToString()
        };
    }

    public string GetKeyAlgorithmDisplayName(CsrKeyAlgorithm algorithm)
    {
        return _csrService.GetKeyAlgorithmDisplayName(algorithm);
    }

    public string GetSignatureAlgorithmDisplayName(CsrSignatureAlgorithm algorithm)
    {
        return _csrService.GetSignatureAlgorithmDisplayName(algorithm);
    }
}
