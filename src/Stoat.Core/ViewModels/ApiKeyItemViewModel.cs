using Stoat.Services.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Stoat.Core.ViewModels;

/// <summary>
/// ViewModel for a single API key item in the list.
/// </summary>
public partial class ApiKeyItemViewModel : ObservableObject
{
    /// <summary>
    /// Unique identifier for the API key.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Human-readable name for the API key.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of the API key (Development or Production).
    /// </summary>
    public ApiKeyType Type { get; set; }

    /// <summary>
    /// Type display string (DEVELOPMENT or PRODUCTION).
    /// </summary>
    public string TypeDisplay => Type switch
    {
        ApiKeyType.Test => "TEST",
        ApiKeyType.Production => "PRODUCTION",
        _ => "DEVELOPMENT"
    };

    /// <summary>
    /// Whether this is a production key (for styling).
    /// </summary>
    public bool IsProduction => Type == ApiKeyType.Production;

    public bool IsTest => Type == ApiKeyType.Test;

    public bool IsDevelopment => Type == ApiKeyType.Development;

    /// <summary>
    /// Length of the key in characters.
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// Complexity level used for generation.
    /// </summary>
    public ApiKeyComplexity Complexity { get; set; }

    /// <summary>
    /// Masked preview of the key value.
    /// </summary>
    public string MaskedPreview { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the API key was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }
}
