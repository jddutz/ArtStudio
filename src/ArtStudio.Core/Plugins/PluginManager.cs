using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using ArtStudio.Core;
using Microsoft.Extensions.Logging;

namespace ArtStudio.Core.Services;

/// <summary>
/// Implementation of plugin management functionality
/// </summary>
public class PluginManager : IPluginManager
{
    private readonly ILogger<PluginManager>? _logger;

    // High-performance logging delegates
    private static readonly Action<ILogger, int, Exception?> LogStartingPluginLoading =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(1, nameof(LogStartingPluginLoading)),
            "Starting plugin loading from {DirectoryCount} directories");

    private static readonly Action<ILogger, string, Exception?> LogPluginDirectoryNotExists =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(2, nameof(LogPluginDirectoryNotExists)),
            "Plugin directory does not exist: {Directory}");

    private static readonly Action<ILogger, int, int, Exception?> LogPluginLoadingCompleted =
        LoggerMessage.Define<int, int>(LogLevel.Information, new EventId(3, nameof(LogPluginLoadingCompleted)),
            "Plugin loading completed. Loaded: {LoadedCount}, Errors: {ErrorCount}");

    private static readonly Action<ILogger, string, Exception?> LogFailedToLoadPlugin =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(4, nameof(LogFailedToLoadPlugin)),
            "Failed to load plugin from file: {FilePath}");

    private static readonly Action<ILogger, string?, Exception?> LogPluginMissingMetadata =
        LoggerMessage.Define<string?>(LogLevel.Warning, new EventId(5, nameof(LogPluginMissingMetadata)),
            "Plugin type {TypeName} is missing PluginMetadata attribute");

    private static readonly Action<ILogger, string, string, Exception?> LogPluginLoadedSuccessfully =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(6, nameof(LogPluginLoadedSuccessfully)),
            "Successfully loaded plugin: {PluginName} (ID: {PluginId})");

    private static readonly Action<ILogger, string?, Exception?> LogFailedToCreatePluginInstance =
        LoggerMessage.Define<string?>(LogLevel.Error, new EventId(7, nameof(LogFailedToCreatePluginInstance)),
            "Failed to create plugin instance for type: {TypeName}");

    private static readonly Action<ILogger, string, Exception?> LogFailedToLoadAssembly =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(8, nameof(LogFailedToLoadAssembly)),
            "Failed to load assembly from file: {FilePath}");

    private static readonly Action<ILogger, string?, Exception?> LogFailedToCreateInstance =
        LoggerMessage.Define<string?>(LogLevel.Error, new EventId(9, nameof(LogFailedToCreateInstance)),
            "Failed to create instance of plugin type: {TypeName}");

    private static readonly Action<ILogger, string, Exception?> LogPluginEnabled =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(10, nameof(LogPluginEnabled)),
            "Plugin enabled: {PluginId}");

    private static readonly Action<ILogger, string, Exception?> LogPluginDisabled =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(11, nameof(LogPluginDisabled)),
            "Plugin disabled: {PluginId}");

    private static readonly Action<ILogger, string, Exception?> LogPluginUnloaded =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(12, nameof(LogPluginUnloaded)),
            "Plugin unloaded: {PluginId}");

    private static readonly Action<ILogger, string, Exception?> LogFailedToUnloadPlugin =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(13, nameof(LogFailedToUnloadPlugin)),
            "Failed to unload plugin: {PluginId}");

    private static readonly Action<ILogger, string, Exception?> LogRegisteredPluginFactory =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(14, nameof(LogRegisteredPluginFactory)),
            "Registered plugin factory for type: {TypeName}");
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
        await Task.Run(() =>
        {
            var loadedPlugins = new List<PluginMetadata>();
            var loadErrors = new List<PluginLoadError>();

            if (_logger != null)
                LogStartingPluginLoading(_logger, pluginDirectories.Length, null);

            foreach (var directory in pluginDirectories)
            {
                if (!Directory.Exists(directory))
                {
                    if (_logger != null)
                        LogPluginDirectoryNotExists(_logger, directory, null);
                    continue;
                }

                LoadPluginsFromDirectory(directory, loadedPlugins, loadErrors);
            }

            if (_logger != null)
                LogPluginLoadingCompleted(_logger, loadedPlugins.Count, loadErrors.Count, null);

            var loadedCollection = new Collection<PluginMetadata>(loadedPlugins);
            var errorsCollection = new Collection<PluginLoadError>(loadErrors);
            PluginsLoaded?.Invoke(this, new PluginsLoadedEventArgs(loadedCollection, errorsCollection));
        }).ConfigureAwait(false);
    }

    private void LoadPluginsFromDirectory(string directory, List<PluginMetadata> loadedPlugins, List<PluginLoadError> loadErrors)
    {
        var dllFiles = Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories);

        foreach (var dllFile in dllFiles)
        {
            try
            {
                LoadPluginFromFile(dllFile, loadedPlugins, loadErrors);
            }
            catch (Exception ex)
            {
                if (_logger != null)
                    LogFailedToLoadPlugin(_logger, dllFile, ex);
                loadErrors.Add(new PluginLoadError
                {
                    FilePath = dllFile,
                    ErrorMessage = ex.Message,
                    Exception = ex,
                    ErrorType = PluginLoadErrorType.Unknown
                });
                throw;
            }
        }
    }

    private void LoadPluginFromFile(string filePath, List<PluginMetadata> loadedPlugins, List<PluginLoadError> loadErrors)
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
                        if (_logger != null)
                            LogPluginMissingMetadata(_logger, pluginType.FullName, null);
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

                        if (_logger != null)
                            LogPluginLoadedSuccessfully(_logger, metadata.Name, metadata.Id, null);
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                        LogFailedToCreatePluginInstance(_logger, pluginType.FullName, ex);
                    loadErrors.Add(new PluginLoadError
                    {
                        FilePath = filePath,
                        ErrorMessage = ex.Message,
                        Exception = ex,
                        ErrorType = PluginLoadErrorType.InitializationFailed
                    });
                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            if (_logger != null)
                LogFailedToLoadAssembly(_logger, filePath, ex);
            loadErrors.Add(new PluginLoadError
            {
                FilePath = filePath,
                ErrorMessage = ex.Message,
                Exception = ex,
                ErrorType = PluginLoadErrorType.InvalidAssembly
            });
            throw;
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
            if (_logger != null)
                LogFailedToCreateInstance(_logger, pluginType.FullName, ex);
            throw;
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

    public Task<bool> EnablePluginAsync(string pluginId)
    {
        if (_plugins.TryGetValue(pluginId, out var plugin) && _pluginMetadata.TryGetValue(pluginId, out var metadata))
        {
            plugin.IsEnabled = true;
            metadata.IsEnabled = true;
            PluginStateChanged?.Invoke(this, new PluginStateChangedEventArgs(pluginId, true));

            if (_logger != null)
                LogPluginEnabled(_logger, pluginId, null);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> DisablePluginAsync(string pluginId)
    {
        if (_plugins.TryGetValue(pluginId, out var plugin) && _pluginMetadata.TryGetValue(pluginId, out var metadata))
        {
            plugin.IsEnabled = false;
            metadata.IsEnabled = false;
            PluginStateChanged?.Invoke(this, new PluginStateChangedEventArgs(pluginId, false));

            if (_logger != null)
                LogPluginDisabled(_logger, pluginId, null);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> UnloadPluginAsync(string pluginId)
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

                if (_logger != null)
                    LogPluginUnloaded(_logger, pluginId, null);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                if (_logger != null)
                    LogFailedToUnloadPlugin(_logger, pluginId, ex);
                throw;
            }
        }
        return Task.FromResult(false);
    }

    public async Task<bool> ReloadPluginAsync(string pluginId)
    {
        if (_pluginMetadata.TryGetValue(pluginId, out var metadata))
        {
            var filePath = metadata.FilePath;
            var unloadResult = await UnloadPluginAsync(pluginId).ConfigureAwait(false);

            if (unloadResult)
            {
                var loadedPlugins = new List<PluginMetadata>();
                var loadErrors = new List<PluginLoadError>();
                LoadPluginFromFile(filePath, loadedPlugins, loadErrors);

                return loadedPlugins.Any(p => p.Id == pluginId);
            }
        }
        return false;
    }

    public Task<PluginInstallResult> InstallPluginAsync(string pluginFilePath)
    {
        // This would copy the plugin to the plugin directory and load it
        // Implementation depends on your plugin deployment strategy
        throw new NotImplementedException("Plugin installation not yet implemented");
    }

    public Task<bool> UninstallPluginAsync(string pluginId)
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
        if (_logger != null)
            LogRegisteredPluginFactory(_logger, typeof(T).Name, null);
    }
}

/// <summary>
/// Plugin context implementation
/// </summary>
internal sealed class PluginContext : IPluginContext
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
