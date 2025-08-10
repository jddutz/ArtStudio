using System.Collections.Generic;

namespace ArtStudio.Core;

/// <summary>
/// Imported document data
/// </summary>
public class ImportedDocument
{
    public int Width { get; set; }
    public int Height { get; set; }
    public double Dpi { get; set; } = 96.0;
    public List<ImportedLayer> Layers { get; set; } = new();
    public Dictionary<string, object> Properties { get; set; } = new();
}
