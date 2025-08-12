using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using ArtStudio.Core.Commands;

namespace ArtStudio.Core.Services;

/// <summary>
/// Manages command plugins and their registration with the command registry
/// </summary>
public class CommandPluginManager
{
    private readonly ICommandRegistry _commandRegistry;
    private readonly IPluginManager _pluginManager;
    private readonly ILogger<CommandPluginManager>? _logger;
    private readonly Dictionary<string, ICommandPlugin> _commandPlugins = new();

    // High-performance logging delegates
    private static readonly Action<ILogger, string, Exception?> LogRegisteringCommandPlugin =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1, nameof(RegisterCommandPlugin)),
            "Registering command plugin: {PluginId}");

    private static readonly Action<ILogger, string, Exception?> LogUnregisteringCommandPlugin =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(2, nameof(UnregisterCommandPlugin)),
            "Unregistering command plugin: {PluginId}");

    private static readonly Action<ILogger, string, Exception?> LogCommandPluginRegistered =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(3, nameof(RegisterCommandPlugin)),
            "Command plugin registered successfully: {PluginId}");

    private static readonly Action<ILogger, string, Exception?> LogCommandPluginUnregistered =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(4, nameof(UnregisterCommandPlugin)),
            "Command plugin unregistered successfully: {PluginId}");

    private static readonly Action<ILogger, string, Exception?> LogFailedToRegisterCommandPlugin =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(5, nameof(RegisterCommandPlugin)),
            "Failed to register command plugin: {PluginId}");

    private static readonly Action<ILogger, string, Exception?> LogFailedToUnregisterCommandPlugin =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6, nameof(UnregisterCommandPlugin)),
            "Failed to unregister command plugin: {PluginId}");

    private static readonly Action<ILogger, Exception?> LogPluginStateChangeHandled =
        LoggerMessage.Define(LogLevel.Debug, new EventId(7, nameof(OnPluginStateChanged)),
            "Plugin state change handled");

    private static readonly Action<ILogger, string, Exception?> LogFailedToHandlePluginStateChange =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(8, nameof(OnPluginStateChanged)),
            "Failed to handle plugin state change for plugin: {PluginId}");

    /// <summary>
    /// Initializes a new instance of CommandPluginManager
    /// </summary>
    public CommandPluginManager(ICommandRegistry commandRegistry, IPluginManager pluginManager, ILogger<CommandPluginManager>? logger = null)
    {
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
        _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
        _logger = logger;

        // Subscribe to plugin state changes
        _pluginManager.PluginStateChanged += OnPluginStateChanged;
    }

    /// <summary>
    /// Registers all available command plugins with the command registry
    /// </summary>
    public void RegisterAllCommandPlugins()
    {
        var commandPlugins = _pluginManager.GetPlugins<ICommandPlugin>();
        foreach (var plugin in commandPlugins)
        {
            var metadata = _pluginManager.GetPluginMetadata(plugin.Id);
            if (metadata?.IsEnabled == true)
            {
                RegisterCommandPlugin(plugin.Id, plugin);
            }
        }
    }

    /// <summary>
    /// Registers a specific command plugin
    /// </summary>
    public void RegisterCommandPlugin(string pluginId, ICommandPlugin commandPlugin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        ArgumentNullException.ThrowIfNull(commandPlugin);

        try
        {
            if (_logger != null)
                LogRegisteringCommandPlugin(_logger, pluginId, null);

            commandPlugin.RegisterCommands(_commandRegistry);
            _commandPlugins[pluginId] = commandPlugin;

            if (_logger != null)
                LogCommandPluginRegistered(_logger, pluginId, null);
        }
        catch (Exception ex)
        {
            if (_logger != null)
                LogFailedToRegisterCommandPlugin(_logger, pluginId, ex);
            throw;
        }
    }

    /// <summary>
    /// Unregisters a specific command plugin
    /// </summary>
    public void UnregisterCommandPlugin(string pluginId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);

        if (!_commandPlugins.TryGetValue(pluginId, out var commandPlugin))
            return;

        try
        {
            if (_logger != null)
                LogUnregisteringCommandPlugin(_logger, pluginId, null);

            commandPlugin.UnregisterCommands(_commandRegistry);
            _commandPlugins.Remove(pluginId);

            if (_logger != null)
                LogCommandPluginUnregistered(_logger, pluginId, null);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        // Catching all exceptions is intentional here because plugin unregistration should not fail
        // the entire application. We log the error and continue gracefully to maintain system stability.
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            if (_logger != null)
                LogFailedToUnregisterCommandPlugin(_logger, pluginId, ex);
            // Don't rethrow - plugin cleanup should be graceful
        }
    }

    /// <summary>
    /// Gets all registered command plugins
    /// </summary>
    public IEnumerable<ICommandPlugin> GetRegisteredCommandPlugins()
    {
        return _commandPlugins.Values.ToList();
    }

    /// <summary>
    /// Gets a specific command plugin by ID
    /// </summary>
    public ICommandPlugin? GetCommandPlugin(string pluginId)
    {
        _commandPlugins.TryGetValue(pluginId, out var plugin);
        return plugin;
    }

    /// <summary>
    /// Checks if a command plugin is registered
    /// </summary>
    public bool IsCommandPluginRegistered(string pluginId)
    {
        return _commandPlugins.ContainsKey(pluginId);
    }

    /// <summary>
    /// Unregisters all command plugins
    /// </summary>
    public void UnregisterAllCommandPlugins()
    {
        var pluginIds = _commandPlugins.Keys.ToList();
        foreach (var pluginId in pluginIds)
        {
            UnregisterCommandPlugin(pluginId);
        }
    }

    /// <summary>
    /// Handles plugin state changes to automatically register/unregister command plugins
    /// </summary>
    private void OnPluginStateChanged(object? sender, PluginStateChangedEventArgs e)
    {
        try
        {
            var plugin = _pluginManager.GetPlugin(e.PluginId);
            if (plugin is not ICommandPlugin commandPlugin)
                return;

            if (e.IsEnabled)
            {
                if (!IsCommandPluginRegistered(e.PluginId))
                {
                    RegisterCommandPlugin(e.PluginId, commandPlugin);
                }
            }
            else
            {
                if (IsCommandPluginRegistered(e.PluginId))
                {
                    UnregisterCommandPlugin(e.PluginId);
                }
            }

            if (_logger != null)
                LogPluginStateChangeHandled(_logger, null);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        // Catching all exceptions is intentional here because event handlers should not fail
        // the entire application. We log the error and continue gracefully.
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            if (_logger != null)
                LogFailedToHandlePluginStateChange(_logger, e.PluginId, ex);
            // Don't rethrow - event handlers should be resilient
        }
    }

    /// <summary>
    /// Disposes resources and unregisters from events
    /// </summary>
    public void Dispose()
    {
        UnregisterAllCommandPlugins();
        _pluginManager.PluginStateChanged -= OnPluginStateChanged;
    }
}
