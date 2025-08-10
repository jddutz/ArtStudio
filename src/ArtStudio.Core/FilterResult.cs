namespace ArtStudio.Core;

/// <summary>
/// Result of a filter operation
/// </summary>
public class FilterResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public LayerData? ResultData { get; set; }
    public FilterMetadata? Metadata { get; set; }
}
