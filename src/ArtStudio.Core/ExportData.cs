using System.Collections.Generic;

namespace ArtStudio.Core;

/// <summary>
/// Data to be exported
/// </summary>
public class ExportData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public double Dpi { get; set; } = 96.0;
    public List<ExportLayer> Layers { get; set; } = new();
    public Dictionary<string, object> Properties { get; set; } = new();
}
