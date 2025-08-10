using System;
using System.Collections.Generic;

namespace ArtStudio.Core;

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
