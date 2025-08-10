using System;
using System.Collections.Generic;

namespace ArtStudio.Core;

/// <summary>
/// Plugin metadata information
/// </summary>
public class PluginMetadata
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Version Version { get; set; } = new Version("1.0.0");
    public string Author { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? License { get; set; }
    public Type[]? Dependencies { get; set; }
    public string[]? SupportedFormats { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTime LoadedDate { get; set; }
    public bool IsEnabled { get; set; }
    public Dictionary<string, object> CustomProperties { get; set; } = new();
}
