using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtStudio.Core;

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
