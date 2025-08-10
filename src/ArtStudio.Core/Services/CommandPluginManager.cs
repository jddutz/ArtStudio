using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using Microsoft.Extensions.Logging;

namespace ArtStudio.Core.Services;

/// <summary>
/// Service that manages command plugins and integrates them with the command registry
/// </summary>
public class CommandPluginManager
{
    private readonly ICommandRegistry _commandRegistry;
    private readonly IPluginManager _pluginManager;
    private readonly ILogger<CommandPluginManager>? _logger;
    private readonly HashSet<string> _registeredPlugins = new();

    /// <summary>
    /// Initialize the command plugin manager
    /// </summary>
    public CommandPluginManager(
        ICommandRegistry commandRegistry,
        IPluginManager pluginManager,
        ILogger<CommandPluginManager>? logger = null)
    {
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
        _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
        _logger = logger;

        // Subscribe to plugin manager events
        _pluginManager.PluginsLoaded += OnPluginsLoaded;
        _pluginManager.PluginStateChanged += OnPluginStateChanged;
    }

    /// <summary>
    /// Register commands from all loaded command plugins
    /// </summary>
    public void RegisterAllCommandPlugins()
    {
        _logger?.LogInformation("Registering commands from all loaded command plugins");

        var commandPlugins = _pluginManager.GetPlugins<ICommandPlugin>();

        foreach (var plugin in commandPlugins)
        {
            RegisterCommandPlugin(plugin);
        }

        _logger?.LogInformation("Registered commands from {Count} command plugins", commandPlugins.Count());
    }

    /// <summary>
    /// Register commands from a specific command plugin
    /// </summary>
    public void RegisterCommandPlugin(ICommandPlugin plugin)
    {
        if (plugin == null)
            throw new ArgumentNullException(nameof(plugin));

        if (_registeredPlugins.Contains(plugin.Id))
        {
            _logger?.LogWarning("Command plugin {PluginId} is already registered", plugin.Id);
            return;
        }

        try
        {
            _logger?.LogDebug("Registering commands from plugin: {PluginId}", plugin.Id);

            plugin.RegisterCommands(_commandRegistry);
            _registeredPlugins.Add(plugin.Id);

            _logger?.LogInformation("Successfully registered commands from plugin: {PluginId}", plugin.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to register commands from plugin: {PluginId}", plugin.Id);
            throw;
        }
    }

    /// <summary>
    /// Unregister commands from a specific command plugin
    /// </summary>
    public void UnregisterCommandPlugin(ICommandPlugin plugin)
    {
        if (plugin == null)
            throw new ArgumentNullException(nameof(plugin));

        if (!_registeredPlugins.Contains(plugin.Id))
        {
            _logger?.LogWarning("Command plugin {PluginId} is not registered", plugin.Id);
            return;
        }

        try
        {
            _logger?.LogDebug("Unregistering commands from plugin: {PluginId}", plugin.Id);

            plugin.UnregisterCommands(_commandRegistry);
            _registeredPlugins.Remove(plugin.Id);

            _logger?.LogInformation("Successfully unregistered commands from plugin: {PluginId}", plugin.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to unregister commands from plugin: {PluginId}", plugin.Id);
        }
    }

    /// <summary>
    /// Get all command plugins
    /// </summary>
    public IEnumerable<ICommandPlugin> GetCommandPlugins()
    {
        return _pluginManager.GetPlugins<ICommandPlugin>();
    }

    /// <summary>
    /// Get commands from a specific plugin
    /// </summary>
    public IEnumerable<IPluginCommand> GetCommandsFromPlugin(string pluginId)
    {
        var plugin = _pluginManager.GetPlugin<ICommandPlugin>(pluginId);
        return plugin?.Commands ?? Enumerable.Empty<IPluginCommand>();
    }

    /// <summary>
    /// Get statistics about command plugins
    /// </summary>
    public CommandPluginStatistics GetStatistics()
    {
        var commandPlugins = GetCommandPlugins().ToList();
        var totalCommands = commandPlugins.SelectMany(p => p.Commands).Count();

        return new CommandPluginStatistics
        {
            TotalCommandPlugins = commandPlugins.Count,
            RegisteredCommandPlugins = _registeredPlugins.Count,
            TotalCommandsFromPlugins = totalCommands,
            CommandPlugins = commandPlugins.Select(p => new CommandPluginInfo
            {
                PluginId = p.Id,
                PluginName = p.Name,
                IsRegistered = _registeredPlugins.Contains(p.Id),
                CommandCount = p.Commands.Count()
            }).ToList()
        };
    }

    /// <summary>
    /// Handle plugins loaded event
    /// </summary>
    private void OnPluginsLoaded(object? sender, PluginsLoadedEventArgs e)
    {
        _logger?.LogDebug("Plugins loaded, registering command plugins");

        // Register commands from all newly loaded command plugins
        RegisterAllCommandPlugins();
    }

    /// <summary>
    /// Handle plugin state changed event
    /// </summary>
    private void OnPluginStateChanged(object? sender, PluginStateChangedEventArgs e)
    {
        var plugin = _pluginManager.GetPlugin(e.PluginId);
        if (plugin is ICommandPlugin commandPlugin)
        {
            _logger?.LogDebug("Command plugin state changed: {PluginId}, enabled: {IsEnabled}",
                commandPlugin.Id, e.IsEnabled);

            try
            {
                if (e.IsEnabled)
                {
                    RegisterCommandPlugin(commandPlugin);
                }
                else
                {
                    UnregisterCommandPlugin(commandPlugin);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to handle state change for command plugin: {PluginId}", commandPlugin.Id);
            }
        }
    }
}

/// <summary>
/// Statistics about command plugins
/// </summary>
public class CommandPluginStatistics
{
    public int TotalCommandPlugins { get; init; }
    public int RegisteredCommandPlugins { get; init; }
    public int TotalCommandsFromPlugins { get; init; }
    public List<CommandPluginInfo> CommandPlugins { get; init; } = new();
}

/// <summary>
/// Information about a command plugin
/// </summary>
public class CommandPluginInfo
{
    public string PluginId { get; init; } = string.Empty;
    public string PluginName { get; init; } = string.Empty;
    public bool IsRegistered { get; init; }
    public int CommandCount { get; init; }
}
