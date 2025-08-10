using System.Collections.Generic;

namespace ArtStudio.Core;

/// <summary>
/// Options for export operations
/// </summary>
public class ExportOptions
{
    public int? Quality { get; set; }
    public bool FlattenLayers { get; set; } = false;
    public bool IncludeMetadata { get; set; } = true;
    public double? Dpi { get; set; }
    public Dictionary<string, object> CustomOptions { get; set; } = new();
}
