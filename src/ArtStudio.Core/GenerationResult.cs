using System.Collections.ObjectModel;

namespace ArtStudio.Core;

/// <summary>
/// Result of an image generation operation
/// </summary>
public class GenerationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public LayerData? GeneratedImage { get; set; }
    public GenerationMetadata? Metadata { get; set; }
    public Collection<LayerData>? Variants { get; }
}
