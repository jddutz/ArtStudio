using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtStudio.Core.Interfaces;

/// <summary>
/// Interface for managing plugins
/// </summary>
public interface IPluginManager
{
    /// <summary>
    /// Event raised when plugins are loaded
    /// </summary>
    event EventHandler<PluginsLoadedEventArgs> PluginsLoaded;

    /// <summary>
    /// Event raised when a plugin is enabled/disabled
    /// </summary>
    event EventHandler<PluginStateChangedEventArgs> PluginStateChanged;

    /// <summary>
    /// Load plugins from the specified directories
    /// </summary>
    Task LoadPluginsAsync(string[] pluginDirectories);

    /// <summary>
    /// Get all loaded plugins
    /// </summary>
    IEnumerable<IPlugin> GetAllPlugins();

    /// <summary>
    /// Get plugins of a specific type
    /// </summary>
    IEnumerable<T> GetPlugins<T>() where T : IPlugin;

    /// <summary>
    /// Get a plugin by ID
    /// </summary>
    IPlugin? GetPlugin(string pluginId);

    /// <summary>
    /// Enable a plugin
    /// </summary>
    Task<bool> EnablePluginAsync(string pluginId);

    /// <summary>
    /// Disable a plugin
    /// </summary>
    Task<bool> DisablePluginAsync(string pluginId);

    /// <summary>
    /// Unload a plugin
    /// </summary>
    Task<bool> UnloadPluginAsync(string pluginId);

    /// <summary>
    /// Reload a plugin
    /// </summary>
    Task<bool> ReloadPluginAsync(string pluginId);

    /// <summary>
    /// Install a plugin from file
    /// </summary>
    Task<PluginInstallResult> InstallPluginAsync(string pluginFilePath);

    /// <summary>
    /// Uninstall a plugin
    /// </summary>
    Task<bool> UninstallPluginAsync(string pluginId);

    /// <summary>
    /// Get plugin metadata
    /// </summary>
    PluginMetadata? GetPluginMetadata(string pluginId);

    /// <summary>
    /// Validate plugin dependencies
    /// </summary>
    ValidationResult ValidatePlugin(string pluginId);

    /// <summary>
    /// Register a plugin type factory
    /// </summary>
    void RegisterPluginFactory<T>(IPluginFactory<T> factory) where T : IPlugin;
}

/// <summary>
/// Plugin factory interface for custom plugin creation
/// </summary>
public interface IPluginFactory<T> where T : IPlugin
{
    /// <summary>
    /// Create a plugin instance
    /// </summary>
    T CreatePlugin(Type pluginType, IPluginContext context);

    /// <summary>
    /// Check if this factory can create the specified type
    /// </summary>
    bool CanCreate(Type pluginType);
}

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

/// <summary>
/// Event arguments for plugins loaded event
/// </summary>
public class PluginsLoadedEventArgs : EventArgs
{
    public List<PluginMetadata> LoadedPlugins { get; }
    public List<PluginLoadError> LoadErrors { get; }

    public PluginsLoadedEventArgs(List<PluginMetadata> loadedPlugins, List<PluginLoadError> loadErrors)
    {
        LoadedPlugins = loadedPlugins;
        LoadErrors = loadErrors;
    }
}

/// <summary>
/// Event arguments for plugin state changed event
/// </summary>
public class PluginStateChangedEventArgs : EventArgs
{
    public string PluginId { get; }
    public bool IsEnabled { get; }

    public PluginStateChangedEventArgs(string pluginId, bool isEnabled)
    {
        PluginId = pluginId;
        IsEnabled = isEnabled;
    }
}

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
