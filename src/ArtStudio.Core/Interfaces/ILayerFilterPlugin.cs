using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArtStudio.Core.Interfaces;

/// <summary>
/// Interface for layer filter/effect plugins
/// </summary>
public interface ILayerFilterPlugin : IPlugin
{
    /// <summary>
    /// Filter category for organization
    /// </summary>
    FilterCategory Category { get; }

    /// <summary>
    /// Icon resource path or identifier
    /// </summary>
    string? IconResource { get; }

    /// <summary>
    /// Whether this filter supports real-time preview
    /// </summary>
    bool SupportsPreview { get; }

    /// <summary>
    /// Whether this filter is destructive (modifies original data)
    /// </summary>
    bool IsDestructive { get; }

    /// <summary>
    /// Filter settings/parameters
    /// </summary>
    IFilterSettings Settings { get; }

    /// <summary>
    /// Apply the filter to layer data
    /// </summary>
    Task<FilterResult> ApplyAsync(LayerData inputData, IFilterSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a preview of the filter effect
    /// </summary>
    Task<FilterResult> PreviewAsync(LayerData inputData, IFilterSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the filter's parameter UI descriptor
    /// </summary>
    FilterParameterDescriptor[] GetParameterDescriptors();
}

/// <summary>
/// Filter categories
/// </summary>
public enum FilterCategory
{
    Blur,
    Sharpen,
    Noise,
    Distort,
    Color,
    Artistic,
    Stylize,
    Texture,
    Light,
    Custom
}

/// <summary>
/// Interface for filter settings
/// </summary>
public interface IFilterSettings
{
    /// <summary>
    /// Get all settings as key-value pairs
    /// </summary>
    Dictionary<string, object> GetSettings();

    /// <summary>
    /// Set a setting value
    /// </summary>
    void SetSetting(string key, object value);

    /// <summary>
    /// Get a setting value
    /// </summary>
    T? GetSetting<T>(string key, T? defaultValue = default);

    /// <summary>
    /// Reset all settings to defaults
    /// </summary>
    void ResetToDefaults();

    /// <summary>
    /// Clone the settings
    /// </summary>
    IFilterSettings Clone();

    /// <summary>
    /// Event raised when settings change
    /// </summary>
    event EventHandler<SettingsChangedEventArgs> SettingsChanged;
}

/// <summary>
/// Layer data for filter operations
/// </summary>
public class LayerData
{
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public int Width { get; set; }
    public int Height { get; set; }
    public PixelFormat Format { get; set; } = PixelFormat.Rgba32;
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Result of a filter operation
/// </summary>
public class FilterResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public LayerData? ResultData { get; set; }
    public FilterMetadata? Metadata { get; set; }
}

/// <summary>
/// Filter metadata
/// </summary>
public class FilterMetadata
{
    public TimeSpan ProcessingTime { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Pixel format enumeration
/// </summary>
public enum PixelFormat
{
    Rgba32,
    Rgb24,
    Bgra32,
    Bgr24,
    Gray8,
    Gray16
}

/// <summary>
/// Filter parameter descriptor for UI generation
/// </summary>
public class FilterParameterDescriptor
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ParameterType Type { get; set; }
    public object? DefaultValue { get; set; }
    public object? MinValue { get; set; }
    public object? MaxValue { get; set; }
    public object[]? AllowedValues { get; set; }
    public string? Unit { get; set; }
    public int DecimalPlaces { get; set; } = 0;
}

/// <summary>
/// Parameter types for filter settings
/// </summary>
public enum ParameterType
{
    Integer,
    Float,
    Boolean,
    String,
    Color,
    Enum,
    Range,
    Point,
    Size
}
