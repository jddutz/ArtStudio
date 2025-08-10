namespace ArtStudio.Core;

/// <summary>
/// Imported layer data
/// </summary>
public class ImportedLayer
{
    public string Name { get; set; } = string.Empty;
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public float Opacity { get; set; } = 1.0f;
    public bool Visible { get; set; } = true;
    public string BlendMode { get; set; } = "Normal";
}
