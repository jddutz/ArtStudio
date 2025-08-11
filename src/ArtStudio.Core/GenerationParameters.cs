using System.Collections.Generic;

namespace ArtStudio.Core;

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
    public Dictionary<string, object> CustomParameters { get; } = new();
}
