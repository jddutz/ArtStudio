using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ArtStudio.Core;

/// <summary>
/// Data to be exported
/// </summary>
public class ExportData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public double Dpi { get; set; } = 96.0;
    public Collection<ExportLayer> Layers { get; } = new();
    public Dictionary<string, object> Properties { get; } = new();
}
