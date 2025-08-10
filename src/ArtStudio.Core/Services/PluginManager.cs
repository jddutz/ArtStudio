using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using ArtStudio.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ArtStudio.Core.Services;

/// <summary>
/// Implementation of plugin management functionality
/// </summary>
public class PluginManager : IPluginManager
{
    private readonly ILogger<PluginManager>? _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, IPlugin> _plugins = new();
    private readonly ConcurrentDictionary<string, PluginMetadata> _pluginMetadata = new();
    private readonly ConcurrentDictionary<string, AssemblyLoadContext> _loadContexts = new();
    private readonly Dictionary<Type, object> _pluginFactories = new();

    public event EventHandler<PluginsLoadedEventArgs>? PluginsLoaded;
    public event EventHandler<PluginStateChangedEventArgs>? PluginStateChanged;

    public PluginManager(IServiceProvider serviceProvider, ILogger<PluginManager>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task LoadPluginsAsync(string[] pluginDirectories)
    {
        var loadedPlugins = new List<PluginMetadata>();
        var loadErrors = new List<PluginLoadError>();

        _logger?.LogInformation("Starting plugin loading from {DirectoryCount} directories", pluginDirectories.Length);

        foreach (var directory in pluginDirectories)
        {
            if (!Directory.Exists(directory))
            {
                _logger?.LogWarning("Plugin directory does not exist: {Directory}", directory);
                continue;
            }

            await LoadPluginsFromDirectoryAsync(directory, loadedPlugins, loadErrors);
        }

        _logger?.LogInformation("Plugin loading completed. Loaded: {LoadedCount}, Errors: {ErrorCount}",
            loadedPlugins.Count, loadErrors.Count);

        PluginsLoaded?.Invoke(this, new PluginsLoadedEventArgs(loadedPlugins, loadErrors));
    }

    private async Task LoadPluginsFromDirectoryAsync(string directory, List<PluginMetadata> loadedPlugins, List<PluginLoadError> loadErrors)
    {
        var dllFiles = Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories);

        foreach (var dllFile in dllFiles)
        {
            try
            {
                await LoadPluginFromFileAsync(dllFile, loadedPlugins, loadErrors);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load plugin from file: {FilePath}", dllFile);
                loadErrors.Add(new PluginLoadError
                {
                    FilePath = dllFile,
                    ErrorMessage = ex.Message,
                    Exception = ex,
                    ErrorType = PluginLoadErrorType.Unknown
                });
            }
        }
    }

    private async Task LoadPluginFromFileAsync(string filePath, List<PluginMetadata> loadedPlugins, List<PluginLoadError> loadErrors)
    {
        try
        {
            var loadContext = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(filePath), true);
            var assembly = loadContext.LoadFromAssemblyPath(filePath);

            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToArray();

            foreach (var pluginType in pluginTypes)
            {
                try
                {
                    var metadataAttribute = pluginType.GetCustomAttribute<PluginMetadataAttribute>();
                    if (metadataAttribute == null)
                    {
                        _logger?.LogWarning("Plugin type {TypeName} is missing PluginMetadata attribute", pluginType.FullName);
                        continue;
                    }

                    var metadata = new PluginMetadata
                    {
                        Id = metadataAttribute.Id,
                        Name = metadataAttribute.Name,
                        Description = metadataAttribute.Description,
                        Author = metadataAttribute.Author,
                        Version = Version.Parse(metadataAttribute.Version),
                        Dependencies = metadataAttribute.Dependencies,
                        SupportedFormats = metadataAttribute.SupportedFormats,
                        FilePath = filePath,
                        LoadedDate = DateTime.UtcNow,
                        IsEnabled = true
                    };

                    var context = new PluginContext(_serviceProvider, _serviceProvider.GetService(typeof(IConfigurationManager)) as IConfigurationManager ?? throw new InvalidOperationException("IConfigurationManager not registered"));
                    var plugin = CreatePluginInstance(pluginType, context);

                    if (plugin != null)
                    {
                        _plugins[metadata.Id] = plugin;
                        _pluginMetadata[metadata.Id] = metadata;
                        _loadContexts[metadata.Id] = loadContext;

                        plugin.Initialize(context);
                        loadedPlugins.Add(metadata);

                        _logger?.LogInformation("Successfully loaded plugin: {PluginName} (ID: {PluginId})",
                            metadata.Name, metadata.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to create plugin instance for type: {TypeName}", pluginType.FullName);
                    loadErrors.Add(new PluginLoadError
                    {
                        FilePath = filePath,
                        ErrorMessage = ex.Message,
                        Exception = ex,
                        ErrorType = PluginLoadErrorType.InitializationFailed
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load assembly from file: {FilePath}", filePath);
            loadErrors.Add(new PluginLoadError
            {
                FilePath = filePath,
                ErrorMessage = ex.Message,
                Exception = ex,
                ErrorType = PluginLoadErrorType.InvalidAssembly
            });
        }
    }

    private IPlugin? CreatePluginInstance(Type pluginType, IPluginContext context)
    {
        // Try registered factories first
        foreach (var (factoryType, factory) in _pluginFactories)
        {
            if (factoryType.IsAssignableFrom(pluginType))
            {
                var factoryMethod = factory.GetType().GetMethod("CreatePlugin");
                if (factoryMethod != null)
                {
                    return factoryMethod.Invoke(factory, new object[] { pluginType, context }) as IPlugin;
                }
            }
        }

        // Default creation
        try
        {
            return Activator.CreateInstance(pluginType) as IPlugin;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create instance of plugin type: {TypeName}", pluginType.FullName);
            return null;
        }
    }

    public IEnumerable<IPlugin> GetAllPlugins()
    {
        return _plugins.Values.Where(p => p.IsEnabled);
    }

    public IEnumerable<T> GetPlugins<T>() where T : IPlugin
    {
        return _plugins.Values.OfType<T>().Where(p => p.IsEnabled);
    }

    public IPlugin? GetPlugin(string pluginId)
    {
        _plugins.TryGetValue(pluginId, out var plugin);
        return plugin?.IsEnabled == true ? plugin : null;
    }

    public async Task<bool> EnablePluginAsync(string pluginId)
    {
        if (_plugins.TryGetValue(pluginId, out var plugin) && _pluginMetadata.TryGetValue(pluginId, out var metadata))
        {
            plugin.IsEnabled = true;
            metadata.IsEnabled = true;
            PluginStateChanged?.Invoke(this, new PluginStateChangedEventArgs(pluginId, true));

            _logger?.LogInformation("Plugin enabled: {PluginId}", pluginId);
            return true;
        }
        return false;
    }

    public async Task<bool> DisablePluginAsync(string pluginId)
    {
        if (_plugins.TryGetValue(pluginId, out var plugin) && _pluginMetadata.TryGetValue(pluginId, out var metadata))
        {
            plugin.IsEnabled = false;
            metadata.IsEnabled = false;
            PluginStateChanged?.Invoke(this, new PluginStateChangedEventArgs(pluginId, false));

            _logger?.LogInformation("Plugin disabled: {PluginId}", pluginId);
            return true;
        }
        return false;
    }

    public async Task<bool> UnloadPluginAsync(string pluginId)
    {
        if (_plugins.TryGetValue(pluginId, out var plugin))
        {
            try
            {
                plugin.Dispose();
                _plugins.TryRemove(pluginId, out _);
                _pluginMetadata.TryRemove(pluginId, out _);

                if (_loadContexts.TryRemove(pluginId, out var loadContext))
                {
                    loadContext.Unload();
                }

                _logger?.LogInformation("Plugin unloaded: {PluginId}", pluginId);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to unload plugin: {PluginId}", pluginId);
                return false;
            }
        }
        return false;
    }

    public async Task<bool> ReloadPluginAsync(string pluginId)
    {
        if (_pluginMetadata.TryGetValue(pluginId, out var metadata))
        {
            var filePath = metadata.FilePath;
            await UnloadPluginAsync(pluginId);

            var loadedPlugins = new List<PluginMetadata>();
            var loadErrors = new List<PluginLoadError>();
            await LoadPluginFromFileAsync(filePath, loadedPlugins, loadErrors);

            return loadedPlugins.Any(p => p.Id == pluginId);
        }
        return false;
    }

    public async Task<PluginInstallResult> InstallPluginAsync(string pluginFilePath)
    {
        // This would copy the plugin to the plugin directory and load it
        // Implementation depends on your plugin deployment strategy
        throw new NotImplementedException("Plugin installation not yet implemented");
    }

    public async Task<bool> UninstallPluginAsync(string pluginId)
    {
        // This would remove the plugin files and unload it
        // Implementation depends on your plugin deployment strategy
        throw new NotImplementedException("Plugin uninstallation not yet implemented");
    }

    public PluginMetadata? GetPluginMetadata(string pluginId)
    {
        _pluginMetadata.TryGetValue(pluginId, out var metadata);
        return metadata;
    }

    public ValidationResult ValidatePlugin(string pluginId)
    {
        var result = new ValidationResult { IsValid = true };

        if (_pluginMetadata.TryGetValue(pluginId, out var metadata))
        {
            // Validate dependencies
            if (metadata.Dependencies != null)
            {
                foreach (var dependency in metadata.Dependencies)
                {
                    if (!_plugins.Values.Any(p => p.GetType() == dependency))
                    {
                        result.IsValid = false;
                        result.Errors.Add(new ValidationError
                        {
                            Parameter = "Dependencies",
                            Message = $"Missing dependency: {dependency.Name}",
                            Code = "MISSING_DEPENDENCY"
                        });
                    }
                }
            }
        }
        else
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError
            {
                Parameter = "PluginId",
                Message = "Plugin not found",
                Code = "PLUGIN_NOT_FOUND"
            });
        }

        return result;
    }

    public void RegisterPluginFactory<T>(IPluginFactory<T> factory) where T : IPlugin
    {
        _pluginFactories[typeof(T)] = factory;
        _logger?.LogInformation("Registered plugin factory for type: {TypeName}", typeof(T).Name);
    }
}

/// <summary>
/// Plugin context implementation
/// </summary>
internal class PluginContext : IPluginContext
{
    public IServiceProvider ServiceProvider { get; }
    public IConfigurationManager ConfigurationManager { get; }
    public Dictionary<string, object> PluginData { get; } = new();

    public PluginContext(IServiceProvider serviceProvider, IConfigurationManager configurationManager)
    {
        ServiceProvider = serviceProvider;
        ConfigurationManager = configurationManager;
    }
}
