using System;
using System.Collections.Generic;

namespace ArtStudio.Core.Interfaces;

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

/// <summary>
/// Result of an export operation
/// </summary>
public class ExportResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public ExportMetadata? Metadata { get; set; }
}

/// <summary>
/// Options for import operations
/// </summary>
public class ImportOptions
{
    public int? MaxWidth { get; set; }
    public int? MaxHeight { get; set; }
    public bool PreserveTransparency { get; set; } = true;
    public bool PreserveLayers { get; set; } = true;
    public double? DpiOverride { get; set; }
    public Dictionary<string, object> CustomOptions { get; set; } = new();
}

/// <summary>
/// Options for export operations
/// </summary>
public class ExportOptions
{
    public int? Quality { get; set; }
    public bool FlattenLayers { get; set; } = false;
    public bool IncludeMetadata { get; set; } = true;
    public double? Dpi { get; set; }
    public Dictionary<string, object> CustomOptions { get; set; } = new();
}

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

/// <summary>
/// Layer data for export
/// </summary>
public class ExportLayer
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

/// <summary>
/// Import metadata
/// </summary>
public class ImportMetadata
{
    public Dictionary<string, object> Properties { get; set; } = new();
    public string? ColorProfile { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

/// <summary>
/// Export metadata
/// </summary>
public class ExportMetadata
{
    public long FileSize { get; set; }
    public string? GeneratedHash { get; set; }
    public DateTime ExportDate { get; set; } = DateTime.UtcNow;
}
