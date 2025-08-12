using System.Collections.Generic;

namespace ArtStudio.Core;

/// <summary>
/// Options for import operations
/// </summary>
public class ImportOptions
{
    public int? MaxWidth { get; set; }
    public int? MaxHeight { get; set; }
    public bool PreserveTransparency { get; set; } = true;
    public bool PreserveLayers { get; set; } = true;
    public double? DpiOverride { get; set; }
    public Dictionary<string, object> CustomOptions { get; } = new();
}
