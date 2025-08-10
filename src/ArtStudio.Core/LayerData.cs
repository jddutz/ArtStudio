using System.Collections.Generic;

namespace ArtStudio.Core;

/// <summary>
/// Layer data for filter operations
/// </summary>
public class LayerData
{
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public int Width { get; set; }
    public int Height { get; set; }
    public PixelFormat Format { get; set; } = PixelFormat.Rgba32;
    public Dictionary<string, object> Properties { get; set; } = new();
}
