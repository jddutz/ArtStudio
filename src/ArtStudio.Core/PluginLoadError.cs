using System;

namespace ArtStudio.Core;

/// <summary>
/// Plugin load error information
/// </summary>
public class PluginLoadError
{
    public string FilePath { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public PluginLoadErrorType ErrorType { get; set; }
}
