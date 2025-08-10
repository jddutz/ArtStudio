using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using Microsoft.Extensions.Logging;

namespace ArtStudio.Core.Services;

/// <summary>
/// Implementation of ICommandRegistry for managing plugin commands
/// </summary>
public class CommandRegistry : ICommandRegistry
{
    private readonly ConcurrentDictionary<string, IPluginCommand> _commands = new();
    private readonly ILogger<CommandRegistry>? _logger;

    /// <inheritdoc />
    public IEnumerable<IPluginCommand> Commands => _commands.Values.OrderBy(c => c.Category).ThenBy(c => c.Priority).ThenBy(c => c.DisplayName);

    /// <inheritdoc />
    public event EventHandler<CommandRegisteredEventArgs>? CommandRegistered;

    /// <inheritdoc />
    public event EventHandler<CommandUnregisteredEventArgs>? CommandUnregistered;

    /// <summary>
    /// Initialize the command registry
    /// </summary>
    public CommandRegistry(ILogger<CommandRegistry>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void RegisterCommand(IPluginCommand command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        if (string.IsNullOrWhiteSpace(command.CommandId))
            throw new ArgumentException("Command ID cannot be null or empty", nameof(command));

        if (_commands.TryAdd(command.CommandId, command))
        {
            _logger?.LogInformation("Registered command: {CommandId} ({DisplayName})",
                command.CommandId, command.DisplayName);

            CommandRegistered?.Invoke(this, new CommandRegisteredEventArgs(command));
        }
        else
        {
            var existingCommand = _commands[command.CommandId];
            var message = $"Command with ID '{command.CommandId}' is already registered by '{existingCommand.GetType().Name}'";
            _logger?.LogWarning(message);
            throw new InvalidOperationException(message);
        }
    }

    /// <inheritdoc />
    public bool UnregisterCommand(string commandId)
    {
        if (string.IsNullOrWhiteSpace(commandId))
            return false;

        if (_commands.TryRemove(commandId, out var command))
        {
            _logger?.LogInformation("Unregistered command: {CommandId} ({DisplayName})",
                commandId, command.DisplayName);

            CommandUnregistered?.Invoke(this, new CommandUnregisteredEventArgs(commandId, command));

            // Dispose the command if it's disposable
            try
            {
                command.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error disposing command {CommandId}", commandId);
            }

            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public IPluginCommand? GetCommand(string commandId)
    {
        if (string.IsNullOrWhiteSpace(commandId))
            return null;

        _commands.TryGetValue(commandId, out var command);
        return command;
    }

    /// <inheritdoc />
    public IEnumerable<IPluginCommand> GetCommandsByCategory(CommandCategory category)
    {
        return _commands.Values
            .Where(c => c.Category == category)
            .OrderBy(c => c.Priority)
            .ThenBy(c => c.DisplayName);
    }

    /// <inheritdoc />
    public IEnumerable<IPluginCommand> GetCommandsWithShortcuts()
    {
        return _commands.Values
            .Where(c => !string.IsNullOrWhiteSpace(c.KeyboardShortcut))
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Priority)
            .ThenBy(c => c.DisplayName);
    }

    /// <inheritdoc />
    public bool IsCommandRegistered(string commandId)
    {
        if (string.IsNullOrWhiteSpace(commandId))
            return false;

        return _commands.ContainsKey(commandId);
    }

    /// <inheritdoc />
    public void Clear()
    {
        var commandIds = _commands.Keys.ToList();

        foreach (var commandId in commandIds)
        {
            UnregisterCommand(commandId);
        }

        _logger?.LogInformation("Cleared all registered commands");
    }

    /// <summary>
    /// Get statistics about registered commands
    /// </summary>
    public CommandRegistryStatistics GetStatistics()
    {
        var commands = _commands.Values.ToList();

        return new CommandRegistryStatistics
        {
            TotalCommands = commands.Count,
            EnabledCommands = commands.Count(c => c.IsEnabled),
            VisibleCommands = commands.Count(c => c.IsVisible),
            CommandsWithShortcuts = commands.Count(c => !string.IsNullOrWhiteSpace(c.KeyboardShortcut)),
            CommandsByCategory = commands.GroupBy(c => c.Category)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }
}

/// <summary>
/// Statistics about the command registry
/// </summary>
public class CommandRegistryStatistics
{
    public int TotalCommands { get; init; }
    public int EnabledCommands { get; init; }
    public int VisibleCommands { get; init; }
    public int CommandsWithShortcuts { get; init; }
    public Dictionary<CommandCategory, int> CommandsByCategory { get; init; } = new();
}
