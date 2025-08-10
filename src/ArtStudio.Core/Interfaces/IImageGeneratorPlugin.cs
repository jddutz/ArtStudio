using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArtStudio.Core;

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
