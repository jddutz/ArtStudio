using System;

namespace ArtStudio.Core;

/// <summary>
/// Export metadata
/// </summary>
public class ExportMetadata
{
    public long FileSize { get; set; }
    public string? GeneratedHash { get; set; }
    public DateTime ExportDate { get; set; } = DateTime.UtcNow;
}
