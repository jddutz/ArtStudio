namespace ArtStudio.Core;

/// <summary>
/// Result of an import operation
/// </summary>
public class ImportResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public ImportedDocument? Document { get; set; }
    public ImportMetadata? Metadata { get; set; }
}
