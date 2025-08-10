using System.Collections.Generic;

namespace ArtStudio.Core;

/// <summary>
/// Interface for plugins that provide commands
/// </summary>
public interface ICommandPlugin : IPlugin
{
    /// <summary>
    /// Commands provided by this plugin
    /// </summary>
    IEnumerable<IPluginCommand> Commands { get; }

    /// <summary>
    /// Called when the plugin should register its commands
    /// </summary>
    /// <param name="commandRegistry">Command registry to register commands with</param>
    void RegisterCommands(ICommandRegistry commandRegistry);

    /// <summary>
    /// Called when the plugin should unregister its commands
    /// </summary>
    /// <param name="commandRegistry">Command registry to unregister commands from</param>
    void UnregisterCommands(ICommandRegistry commandRegistry);
}
