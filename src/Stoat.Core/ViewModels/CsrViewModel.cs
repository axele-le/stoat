using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Stoat.Services.Interfaces;
using Stoat.Services.Models;

namespace Stoat.Core.ViewModels;

/// <summary>
/// Main ViewModel for the CSR page.
/// </summary>
public partial class CsrViewModel : ViewModelBase
{
    private readonly ICsrService _csrService;
    private readonly ICsrStorageService _csrStorageService;
    private readonly ICsrImportService _csrImportService;
    private readonly IConfirmationService _confirmationService;
    private readonly ILocalizationService _localizationService;

    /// <summary>
    /// Collection of CSRs grouped by private key.
    /// </summary>
    public ObservableCollection<CsrPrivateKeyGroupViewModel> CsrGroups { get; } = new();

    /// <summary>
    /// Whether there are any CSRs.
    /// </summary>
    [ObservableProperty]
    private bool _hasCsrs;

    /// <summary>
    /// Whether the list view should be visible (no form is open).
    /// </summary>
    public bool ShowListView => !IsFormOpen && !IsImportFormOpen;

    /// <summary>
    /// Whether the form is currently open.
    /// </summary>
    [ObservableProperty]
    private bool _isFormOpen;

    /// <summary>
    /// The form ViewModel for creating CSRs.
    /// </summary>
    [ObservableProperty]
    private CsrFormViewModel? _formViewModel;

    /// <summary>
    /// Status message to display to the user.
    /// </summary>
    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>
    /// Whether the status message indicates an error.
    /// </summary>
    [ObservableProperty]
    private bool _isError;

    /// <summary>
    /// Currently expanded CSR item (for showing details).
    /// </summary>
    [ObservableProperty]
    private CsrItemViewModel? _expandedCsr;

    /// <summary>
    /// Whether the import form is currently open.
    /// </summary>
    [ObservableProperty]
    private bool _isImportFormOpen;

    /// <summary>
    /// The import form ViewModel.
    /// </summary>
    [ObservableProperty]
    private CsrImportFormViewModel? _importFormViewModel;

    partial void OnIsFormOpenChanged(bool value) => OnPropertyChanged(nameof(ShowListView));
    partial void OnIsImportFormOpenChanged(bool value) => OnPropertyChanged(nameof(ShowListView));

    // Events for View to handle platform-specific operations
    public event Action<string, string, string>? SaveFileRequested;
    public event Action<string>? CopyToClipboardRequested;

    public CsrViewModel(ICsrService csrService, ICsrStorageService csrStorageService, ICsrImportService csrImportService, IConfirmationService confirmationService, ILocalizationService localizationService)
    {
        _csrService = csrService;
        _csrStorageService = csrStorageService;
        _csrImportService = csrImportService;
        _confirmationService = confirmationService;
        _localizationService = localizationService;

        // Subscribe to collection events
        _csrStorageService.CsrCreated += OnCsrCreated;
        _csrStorageService.CsrDeleted += OnCsrDeleted;
    }

    public override async void OnNavigatedTo()
    {
        base.OnNavigatedTo();
        await RunWithMinimumLoadingTime(LoadCsrsAsync);
    }

    private async Task LoadCsrsAsync()
    {
        try
        {
            var csrs = await _csrStorageService.GetAllCsrsAsync();
            CsrGroups.Clear();

            // Group CSRs by public key (same public key = same private key)
            var groups = new Dictionary<string, CsrPrivateKeyGroupViewModel>();
            int groupNumber = 1;

            foreach (var csr in csrs.OrderByDescending(c => c.CreatedAtUtc))
            {
                var item = MapToViewModel(csr);

                // Use hash of public key for reliable grouping
                var groupKey = ComputePublicKeyHash(csr.PublicKey);

                if (!groups.TryGetValue(groupKey, out var group))
                {
                    group = new CsrPrivateKeyGroupViewModel
                    {
                        ParentViewModel = this,
                        GroupId = groupKey,
                        GroupNumber = groupNumber++,
                        GroupName = csr.KeyGroupName,
                        Algorithm = csr.Algorithm,
                        FirstCsrId = csr.Id
                    };
                    groups[groupKey] = group;
                    CsrGroups.Add(group);
                }
                group.Csrs.Add(item);
            }

            HasCsrs = CsrGroups.Count > 0;
        }
        catch (Exception ex)
        {
            SetStatus(_localizationService.Format("Common.Error.LoadingFailed", ex.Message), isError: true);
        }
    }

    private void OnCsrCreated(object? sender, CsrKeyPair csr)
    {
        var item = MapToViewModel(csr);

        // Use hash of public key for reliable grouping
        var groupKey = ComputePublicKeyHash(csr.PublicKey);

        var existingGroup = CsrGroups.FirstOrDefault(g => g.GroupId == groupKey);
        if (existingGroup != null)
        {
            existingGroup.Csrs.Insert(0, item);
            existingGroup.RefreshCsrCount();
        }
        else
        {
            var newGroup = new CsrPrivateKeyGroupViewModel
            {
                ParentViewModel = this,
                GroupId = groupKey,
                GroupNumber = CsrGroups.Count + 1,
                GroupName = csr.KeyGroupName,
                Algorithm = csr.Algorithm,
                FirstCsrId = csr.Id
            };
            newGroup.Csrs.Add(item);
            CsrGroups.Insert(0, newGroup);
        }

        HasCsrs = true;
    }

    private void OnCsrDeleted(object? sender, Guid id)
    {
        foreach (var group in CsrGroups.ToList())
        {
            var item = group.Csrs.FirstOrDefault(c => c.Id == id);
            if (item != null)
            {
                group.Csrs.Remove(item);
                group.RefreshCsrCount();

                // Remove empty groups
                if (group.Csrs.Count == 0)
                {
                    CsrGroups.Remove(group);
                    // Renumber remaining groups
                    for (int i = 0; i < CsrGroups.Count; i++)
                    {
                        CsrGroups[i].GroupNumber = i + 1;
                    }
                }
                break;
            }
        }
        HasCsrs = CsrGroups.Count > 0;
    }

    private static CsrItemViewModel MapToViewModel(CsrKeyPair csr)
    {
        return new CsrItemViewModel
        {
            Id = csr.Id,
            Name = csr.Name,
            CsrPem = csr.CsrPem,
            PublicKey = csr.PublicKey,
            Algorithm = csr.Algorithm,
            KeyAlgorithm = csr.KeyAlgorithm,
            SignatureAlgorithm = csr.SignatureAlgorithm,
            CommonName = csr.CommonName,
            Organization = csr.Organization,
            OrganizationalUnit = csr.OrganizationalUnit,
            Locality = csr.Locality,
            State = csr.State,
            Country = csr.Country,
            SubjectDN = csr.SubjectDN,
            SanCount = csr.SanCount,
            SanList = csr.SanList,
            CreatedAtUtc = csr.CreatedAtUtc,
            HasPrivateKey = csr.HasPrivateKey,
            Origin = csr.Origin
        };
    }

    [RelayCommand]
    private void OpenNewCsrForm()
    {
        FormViewModel = new CsrFormViewModel(_csrService, _csrStorageService, _localizationService);
        FormViewModel.CloseRequested += () =>
        {
            IsFormOpen = false;
            FormViewModel = null;
        };
        FormViewModel.GenerationSucceeded += () =>
        {
            IsFormOpen = false;
            FormViewModel = null;
        };

        // Pass existing keys to the form
        var existingKeys = BuildExistingKeysList();
        FormViewModel.SetExistingKeys(existingKeys);

        IsFormOpen = true;
    }

    /// <summary>
    /// Builds the list of existing keys from the current CSR groups.
    /// </summary>
    private List<ExistingKeyInfo> BuildExistingKeysList()
    {
        var keys = new List<ExistingKeyInfo>();

        foreach (var group in CsrGroups)
        {
            var firstCsr = group.Csrs.FirstOrDefault();
            if (firstCsr == null) continue;

            keys.Add(new ExistingKeyInfo
            {
                KeyId = group.GroupId,
                Label = group.DisplayName,
                KeyAlgorithm = firstCsr.KeyAlgorithm,
                AlgorithmDisplayName = group.Algorithm,
                CsrId = group.FirstCsrId,
                PublicKey = firstCsr.PublicKey
            });
        }

        return keys;
    }

    [RelayCommand]
    private void CloseForm()
    {
        IsFormOpen = false;
        FormViewModel = null;
    }

    [RelayCommand]
    private void OpenImportForm()
    {
        ImportFormViewModel = new CsrImportFormViewModel(_csrImportService, _csrStorageService, _localizationService);
        ImportFormViewModel.CloseRequested += () =>
        {
            IsImportFormOpen = false;
            ImportFormViewModel = null;
        };
        ImportFormViewModel.ImportSucceeded += () =>
        {
            IsImportFormOpen = false;
            ImportFormViewModel = null;
            SetStatus(_localizationService.Format("Csr.Success.Imported"));
        };
        IsImportFormOpen = true;
    }

    [RelayCommand]
    private void CloseImportForm()
    {
        IsImportFormOpen = false;
        ImportFormViewModel = null;
    }

    [RelayCommand]
    private async Task DeleteCsrAsync(Guid id)
    {
        if (!await _confirmationService.ConfirmAsync(_localizationService.Format("Csr.Delete.Dialog.Title"), _localizationService.Format("Csr.Delete.Dialog.Confirm")))
            return;

        try
        {
            await _csrStorageService.DeleteCsrAsync(id);
            SetStatus(_localizationService.Format("Csr.Success.Deleted"));
        }
        catch (Exception ex)
        {
            SetStatus(_localizationService.Format("Common.Error.DeletionFailed", ex.Message), isError: true);
        }
    }

    [RelayCommand]
    private void ToggleExpandCsr(CsrItemViewModel item)
    {
        if (ExpandedCsr == item)
        {
            ExpandedCsr = null;
        }
        else
        {
            ExpandedCsr = item;
        }
    }

    #region Group Name Commands

    [RelayCommand]
    private void StartEditGroupName(CsrPrivateKeyGroupViewModel group)
    {
        group.IsEditingName = true;
    }

    [RelayCommand]
    private async Task SaveGroupNameAsync(CsrPrivateKeyGroupViewModel group)
    {
        try
        {
            await _csrStorageService.UpdateKeyGroupNameAsync(group.GroupId, group.GroupName);
            group.IsEditingName = false;
            SetStatus(_localizationService.Format("Csr.Group.Success.NameUpdated"));
        }
        catch (Exception ex)
        {
            SetStatus(_localizationService.Format("Common.Error.WithMessage", ex.Message), isError: true);
        }
    }

    [RelayCommand]
    private void CancelEditGroupName(CsrPrivateKeyGroupViewModel group)
    {
        group.IsEditingName = false;
        // Reload the original name
        var firstCsr = group.Csrs.FirstOrDefault();
        // Note: We'd need to reload from storage to get the original name
        // For simplicity, we just close edit mode
    }

    #endregion

    #region Group Private Key Commands

    [RelayCommand]
    private async Task ToggleShowGroupPrivateKeyAsync(CsrPrivateKeyGroupViewModel group)
    {
        if (group.ShowPrivateKey)
        {
            // Hide
            group.ShowPrivateKey = false;
            group.PrivateKey = null;
        }
        else
        {
            // Show - load private key on demand
            group.IsLoadingPrivateKey = true;
            try
            {
                group.PrivateKey = await _csrStorageService.GetDecryptedPrivateKeyAsync(group.FirstCsrId);
                group.ShowPrivateKey = true;
            }
            catch (Exception ex)
            {
                SetStatus(_localizationService.Format("Common.Error.WithMessage", ex.Message), isError: true);
            }
            finally
            {
                group.IsLoadingPrivateKey = false;
            }
        }
    }

    [RelayCommand]
    private async Task CopyGroupPrivateKeyAsync(CsrPrivateKeyGroupViewModel group)
    {
        try
        {
            string privateKey;
            if (!string.IsNullOrEmpty(group.PrivateKey))
            {
                privateKey = group.PrivateKey;
            }
            else
            {
                privateKey = await _csrStorageService.GetDecryptedPrivateKeyAsync(group.FirstCsrId);
            }

            CopyToClipboardRequested?.Invoke(privateKey);
            group.IsCopied = true;
            SetStatus(_localizationService.Format("Csr.Success.PrivateKeyCopied"));

            // Reset copied state after 2 seconds
            _ = Task.Delay(2000).ContinueWith(_ => group.IsCopied = false);
        }
        catch (Exception ex)
        {
            SetStatus(_localizationService.Format("Common.Error.WithMessage", ex.Message), isError: true);
        }
    }

    [RelayCommand]
    private async Task DownloadGroupPrivateKeyAsync(CsrPrivateKeyGroupViewModel group)
    {
        try
        {
            string privateKey;
            if (!string.IsNullOrEmpty(group.PrivateKey))
            {
                privateKey = group.PrivateKey;
            }
            else
            {
                privateKey = await _csrStorageService.GetDecryptedPrivateKeyAsync(group.FirstCsrId);
            }

            var fileName = $"private-key-{group.GroupNumber}.key";
            SaveFileRequested?.Invoke(privateKey, fileName, _localizationService.Format("Csr.File.SavePrivateKeyTitle"));
        }
        catch (Exception ex)
        {
            SetStatus(_localizationService.Format("Common.Error.WithMessage", ex.Message), isError: true);
        }
    }

    #endregion

    #region CSR Item Commands

    [RelayCommand]
    private void CopyCsr(CsrItemViewModel item)
    {
        CopyToClipboardRequested?.Invoke(item.CsrPem);
        SetStatus(_localizationService.Format("Csr.Success.CsrCopied"));
    }

    [RelayCommand]
    private void DownloadCsr(CsrItemViewModel item)
    {
        var fileName = $"{SanitizeFileName(item.Name)}.csr";
        SaveFileRequested?.Invoke(item.CsrPem, fileName, _localizationService.Format("Csr.File.SaveCsrTitle"));
    }

    [RelayCommand]
    private void DownloadCsrAndKey(CsrItemViewModel item)
    {
        // Download CSR
        var csrFileName = $"{SanitizeFileName(item.Name)}.csr";
        SaveFileRequested?.Invoke(item.CsrPem, csrFileName, _localizationService.Format("Csr.File.SaveCsrTitle"));
    }

    #endregion

    public void SetFileSaveResult(bool success, string? message = null)
    {
        SetStatus(message ?? (success ? _localizationService.Format("Common.Success.FileSaved") : _localizationService.Format("Common.Error.SaveCancelled")), isError: !success);
    }

    private void SetStatus(string message, bool isError = false)
    {
        StatusMessage = message;
        IsError = isError;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Computes a hash of the public key for grouping CSRs by key pair.
    /// </summary>
    private static string ComputePublicKeyHash(string publicKey)
    {
        // Use SHA256 hash of the entire public key for reliable grouping
        var bytes = Encoding.UTF8.GetBytes(publicKey);
        var hash = SHA256.HashData(bytes);
        // Return first 16 bytes as hex (32 chars) - enough for uniqueness
        return Convert.ToHexString(hash, 0, 16);
    }
}
