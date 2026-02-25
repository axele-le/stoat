using CommunityToolkit.Mvvm.ComponentModel;

namespace Stoat.Core.ViewModels;

public partial class ProfileItemViewModel : ObservableObject
{
    public Guid Id { get; set; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private DateTime _createdAt;

    [ObservableProperty]
    private DateTime? _lastUsedAt;

    [ObservableProperty]
    private bool _isActive;

    // Accordion state
    [ObservableProperty]
    private bool _isExpanded;

    // Salt & Secret (loaded on demand when expanded)
    [ObservableProperty]
    private string _salt = string.Empty;

    [ObservableProperty]
    private string _secret = string.Empty;

    [ObservableProperty]
    private bool _showSalt;

    [ObservableProperty]
    private bool _showSecret;

    // Editing state
    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editingName = string.Empty;

    // Export selection
    [ObservableProperty]
    private bool _isSelectedForExport;
}
