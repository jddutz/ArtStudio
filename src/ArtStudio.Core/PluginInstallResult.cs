using System;
using System.Collections.Generic;

namespace ArtStudio.Core;

/// <summary>
/// Plugin installation result
/// </summary>
public class PluginInstallResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public PluginMetadata? InstalledPlugin { get; set; }
    public List<string> Warnings { get; set; } = new();
}
