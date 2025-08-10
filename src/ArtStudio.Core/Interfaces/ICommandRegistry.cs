using System;
using System.Collections.Generic;

namespace ArtStudio.Core;

/// <summary>
/// Interface for managing plugin commands
/// </summary>
public interface ICommandRegistry
{
    /// <summary>
    /// All registered commands
    /// </summary>
    IEnumerable<IPluginCommand> Commands { get; }

    /// <summary>
    /// Event fired when a command is registered
    /// </summary>
    event EventHandler<CommandRegisteredEventArgs>? CommandRegistered;

    /// <summary>
    /// Event fired when a command is unregistered
    /// </summary>
    event EventHandler<CommandUnregisteredEventArgs>? CommandUnregistered;

    /// <summary>
    /// Register a command
    /// </summary>
    /// <param name="command">Command to register</param>
    void RegisterCommand(IPluginCommand command);

    /// <summary>
    /// Unregister a command
    /// </summary>
    /// <param name="commandId">Command ID to unregister</param>
    /// <returns>True if the command was found and unregistered</returns>
    bool UnregisterCommand(string commandId);

    /// <summary>
    /// Get a command by ID
    /// </summary>
    /// <param name="commandId">Command ID</param>
    /// <returns>Command if found, null otherwise</returns>
    IPluginCommand? GetCommand(string commandId);

    /// <summary>
    /// Get commands by category
    /// </summary>
    /// <param name="category">Command category</param>
    /// <returns>Commands in the specified category</returns>
    IEnumerable<IPluginCommand> GetCommandsByCategory(CommandCategory category);

    /// <summary>
    /// Get commands with keyboard shortcuts
    /// </summary>
    /// <returns>Commands that have keyboard shortcuts defined</returns>
    IEnumerable<IPluginCommand> GetCommandsWithShortcuts();

    /// <summary>
    /// Check if a command ID is already registered
    /// </summary>
    /// <param name="commandId">Command ID to check</param>
    /// <returns>True if the command ID is already registered</returns>
    bool IsCommandRegistered(string commandId);

    /// <summary>
    /// Clear all registered commands
    /// </summary>
    void Clear();
}
