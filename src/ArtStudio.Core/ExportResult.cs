namespace ArtStudio.Core;

/// <summary>
/// Result of an export operation
/// </summary>
public class ExportResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public ExportMetadata? Metadata { get; set; }
}
