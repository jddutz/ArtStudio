namespace ArtStudio.Core;

/// <summary>
/// Plugin load error types
/// </summary>
public enum PluginLoadErrorType
{
    FileNotFound,
    InvalidAssembly,
    MissingMetadata,
    DependencyMissing,
    InitializationFailed,
    SecurityError,
    Unknown
}
