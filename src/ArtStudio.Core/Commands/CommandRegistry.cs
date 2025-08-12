using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core.Commands;
using Microsoft.Extensions.Logging;

namespace ArtStudio.Core.Services;

/// <summary>
/// Implementation of ICommandRegistry for managing plugin commands
/// </summary>
public class CommandRegistry : ICommandRegistry
{
    private readonly ConcurrentDictionary<string, IPluginCommand> _commands = new();
    private readonly ILogger<CommandRegistry>? _logger;

    // LoggerMessage delegates for high-performance logging
    private static readonly Action<ILogger, string, string, Exception?> LogRegisteredCommand =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(1, nameof(RegisterCommand)), "Registered command: {CommandId} ({DisplayName})");

    private static readonly Action<ILogger, string, Exception?> LogWarningMessage =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(2, nameof(RegisterCommand)), "{Message}");

    private static readonly Action<ILogger, string, string, Exception?> LogUnregisteredCommand =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(3, nameof(UnregisterCommand)), "Unregistered command: {CommandId} ({DisplayName})");

    private static readonly Action<ILogger, string, Exception?> LogDisposeWarning =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(4, nameof(UnregisterCommand)), "Error disposing command {CommandId}");

    private static readonly Action<ILogger, Exception?> LogClearedCommands =
        LoggerMessage.Define(LogLevel.Information, new EventId(5, nameof(Clear)), "Cleared all registered commands");

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
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.CommandId))
            throw new ArgumentException("Command ID cannot be null or empty", nameof(command));

        if (_commands.TryAdd(command.CommandId, command))
        {
            if (_logger != null)
                LogRegisteredCommand(_logger, command.CommandId, command.DisplayName, null);

            CommandRegistered?.Invoke(this, new CommandRegisteredEventArgs(command));
        }
        else
        {
            var existingCommand = _commands[command.CommandId];
            var message = $"Command with ID '{command.CommandId}' is already registered by '{existingCommand.GetType().Name}'";
            if (_logger != null)
                LogWarningMessage(_logger, message, null);
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
            IDisposable? disposableCommand = null;
            try
            {
                disposableCommand = command as IDisposable;

                if (_logger != null)
                    LogUnregisteredCommand(_logger, commandId, command.DisplayName, null);

                CommandUnregistered?.Invoke(this, new CommandUnregisteredEventArgs(commandId, command));

                // Transfer ownership to avoid dispose in finally
                disposableCommand = null;

                // Dispose the command if it's disposable
                if (command is IDisposable disposable)
                    disposable.Dispose();

                return true;
            }
            catch (ObjectDisposedException)
            {
                // Command already disposed, which is fine
                return true;
            }
            catch (InvalidOperationException ex)
            {
                if (_logger != null)
                    LogDisposeWarning(_logger, commandId, ex);
                return true; // Command was removed even if disposal failed
            }
            finally
            {
                // Ensure disposal in case of exceptions
                disposableCommand?.Dispose();
            }
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

        if (_logger != null)
            LogClearedCommands(_logger, null);
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
