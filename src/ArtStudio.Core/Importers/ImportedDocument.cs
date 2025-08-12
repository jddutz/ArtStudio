using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ArtStudio.Core;

/// <summary>
/// Imported document data
/// </summary>
public class ImportedDocument
{
    public int Width { get; set; }
    public int Height { get; set; }
    public double Dpi { get; set; } = 96.0;
    public Collection<ImportedLayer> Layers { get; } = new();
    public Dictionary<string, object> Properties { get; } = new();
}
