using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArtStudio.Core.Interfaces;

/// <summary>
/// Interface for image generator plugins (procedural and AI)
/// </summary>
public interface IImageGeneratorPlugin : IPlugin
{
    /// <summary>
    /// Generator type (procedural, AI, etc.)
    /// </summary>
    GeneratorType Type { get; }

    /// <summary>
    /// Generator category for organization
    /// </summary>
    GeneratorCategory Category { get; }

    /// <summary>
    /// Icon resource path or identifier
    /// </summary>
    string? IconResource { get; }

    /// <summary>
    /// Whether this generator supports real-time preview
    /// </summary>
    bool SupportsPreview { get; }

    /// <summary>
    /// Whether this generator requires internet connection
    /// </summary>
    bool RequiresInternet { get; }

    /// <summary>
    /// Generator settings/parameters
    /// </summary>
    IGeneratorSettings Settings { get; }

    /// <summary>
    /// Generate an image based on the provided parameters
    /// </summary>
    Task<GenerationResult> GenerateAsync(GenerationParameters parameters, IGeneratorSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a preview of the output
    /// </summary>
    Task<GenerationResult> PreviewAsync(GenerationParameters parameters, IGeneratorSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the generator's parameter UI descriptor
    /// </summary>
    GeneratorParameterDescriptor[] GetParameterDescriptors();

    /// <summary>
    /// Validate generation parameters
    /// </summary>
    ValidationResult ValidateParameters(GenerationParameters parameters, IGeneratorSettings settings);
}

/// <summary>
/// Generator types
/// </summary>
public enum GeneratorType
{
    Procedural,
    AI,
    Hybrid,
    Custom
}

/// <summary>
/// Generator categories
/// </summary>
public enum GeneratorCategory
{
    Texture,
    Pattern,
    Noise,
    Gradient,
    Shape,
    Landscape,
    Portrait,
    Abstract,
    Concept,
    Custom
}

/// <summary>
/// Interface for generator settings
/// </summary>
public interface IGeneratorSettings
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
    IGeneratorSettings Clone();

    /// <summary>
    /// Event raised when settings change
    /// </summary>
    event EventHandler<SettingsChangedEventArgs> SettingsChanged;
}

/// <summary>
/// Parameters for image generation
/// </summary>
public class GenerationParameters
{
    public int Width { get; set; } = 512;
    public int Height { get; set; } = 512;
    public string? Prompt { get; set; }
    public string? NegativePrompt { get; set; }
    public long? Seed { get; set; }
    public float Strength { get; set; } = 1.0f;
    public int Steps { get; set; } = 20;
    public float GuidanceScale { get; set; } = 7.5f;
    public LayerData? InputImage { get; set; }
    public LayerData? MaskImage { get; set; }
    public Dictionary<string, object> CustomParameters { get; set; } = new();
}

/// <summary>
/// Result of an image generation operation
/// </summary>
public class GenerationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public LayerData? GeneratedImage { get; set; }
    public GenerationMetadata? Metadata { get; set; }
    public List<LayerData>? Variants { get; set; }
}

/// <summary>
/// Generation metadata
/// </summary>
public class GenerationMetadata
{
    public TimeSpan GenerationTime { get; set; }
    public long? UsedSeed { get; set; }
    public string? Model { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public string? OriginalPrompt { get; set; }
    public float? ActualGuidanceScale { get; set; }
    public int? ActualSteps { get; set; }
}

/// <summary>
/// Generator parameter descriptor for UI generation
/// </summary>
public class GeneratorParameterDescriptor
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
    public bool IsRequired { get; set; } = false;
    public string? Group { get; set; }
}

/// <summary>
/// Validation result for generation parameters
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
    public List<ValidationWarning> Warnings { get; set; } = new();
}

/// <summary>
/// Validation error
/// </summary>
public class ValidationError
{
    public string Parameter { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// Validation warning
/// </summary>
public class ValidationWarning
{
    public string Parameter { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
