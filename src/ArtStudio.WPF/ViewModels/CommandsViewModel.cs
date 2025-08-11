using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using ArtStudio.Core;
using ArtStudio.Core.Services;
using ArtStudio.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArtStudio.WPF.ViewModels;

/// <summary>
/// Example view model showing how to integrate plugin commands with WPF
/// </summary>
public class CommandsViewModel
{
    private readonly ICommandRegistry _commandRegistry;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CommandsViewModel>? _logger;
    private readonly Dictionary<string, PluginCommandWrapper> _commandWrappers = new();

    // High-performance logging delegates
    private static readonly Action<ILogger, string, Exception?> _logCommandRegisteredDelegate =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, nameof(CommandsViewModel)), "Command registered in view model: {CommandId}");

    private static readonly Action<ILogger, string, Exception?> _logCommandUnregisteredDelegate =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(2, nameof(CommandsViewModel)), "Command unregistered in view model: {CommandId}");

    private static readonly Action<ILogger, string, Exception?> _logCommandNotFoundDelegate =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(3, nameof(CommandsViewModel)), "Command not found: {CommandId}");

    /// <summary>
    /// File menu commands
    /// </summary>
    public IEnumerable<ICommand> FileCommands => GetCommandsByCategory(CommandCategory.File);

    /// <summary>
    /// Edit menu commands
    /// </summary>
    public IEnumerable<ICommand> EditCommands => GetCommandsByCategory(CommandCategory.Edit);

    /// <summary>
    /// Filter menu commands
    /// </summary>
    public IEnumerable<ICommand> FilterCommands => GetCommandsByCategory(CommandCategory.Filter);

    /// <summary>
    /// All available commands
    /// </summary>
    public IEnumerable<ICommand> AllCommands => _commandWrappers.Values;

    /// <summary>
    /// Commands with keyboard shortcuts for binding
    /// </summary>
    public IEnumerable<(string Shortcut, ICommand Command)> ShortcutCommands =>
        _commandRegistry.GetCommandsWithShortcuts()
            .Where(c => !string.IsNullOrWhiteSpace(c.KeyboardShortcut))
            .Select(c => (c.KeyboardShortcut!, (ICommand)GetOrCreateWrapper(c)))
            .ToList();

    /// <summary>
    /// Event fired when command execution starts
    /// </summary>
    public event EventHandler<CommandExecutionEventArgs>? CommandExecutionStarted;

    /// <summary>
    /// Event fired when command execution completes
    /// </summary>
    public event EventHandler<CommandExecutionCompletedEventArgs>? CommandExecutionCompleted;

    /// <summary>
    /// Event fired when command reports progress
    /// </summary>
    public event EventHandler<CommandProgressEventArgs>? CommandProgressReported;

    /// <summary>
    /// Initialize the commands view model
    /// </summary>
    public CommandsViewModel(
        ICommandRegistry commandRegistry,
        IServiceProvider serviceProvider,
        ILogger<CommandsViewModel>? logger = null)
    {
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger;

        // Subscribe to command registry events
        _commandRegistry.CommandRegistered += OnCommandRegistered;
        _commandRegistry.CommandUnregistered += OnCommandUnregistered;

        // Create wrappers for existing commands
        RefreshCommandWrappers();
    }

    /// <summary>
    /// Get commands by category
    /// </summary>
    private IEnumerable<ICommand> GetCommandsByCategory(CommandCategory category)
    {
        return _commandRegistry.GetCommandsByCategory(category)
            .Select(GetOrCreateWrapper)
            .Where(w => w.PluginCommand.IsVisible)
            .OrderBy(w => w.PluginCommand.Priority)
            .ThenBy(w => w.PluginCommand.DisplayName);
    }

    /// <summary>
    /// Get or create a command wrapper
    /// </summary>
    private PluginCommandWrapper GetOrCreateWrapper(IPluginCommand command)
    {
        if (_commandWrappers.TryGetValue(command.CommandId, out var existingWrapper))
            return existingWrapper;

        var context = CreateCommandContext();
        var wrapper = new PluginCommandWrapper(command, context, null, _logger);

        // Subscribe to wrapper events
        wrapper.ExecutionStarted += (s, e) => CommandExecutionStarted?.Invoke(this, e);
        wrapper.ExecutionCompleted += (s, e) => CommandExecutionCompleted?.Invoke(this, e);
        wrapper.ProgressReported += (s, e) => CommandProgressReported?.Invoke(this, e);

        _commandWrappers[command.CommandId] = wrapper;
        return wrapper;
    }

    /// <summary>
    /// Create command execution context
    /// </summary>
    private CommandContext CreateCommandContext()
    {
        var editorService = _serviceProvider.GetRequiredService<IEditorService>();
        var configManager = _serviceProvider.GetRequiredService<IConfigurationManager>();

        return new CommandContext(
            _serviceProvider,
            editorService,
            configManager,
            CommandExecutionMode.Interactive
        );
    }

    /// <summary>
    /// Handle command registration
    /// </summary>
    private void OnCommandRegistered(object? sender, CommandRegisteredEventArgs e)
    {
        if (_logger != null)
        {
            _logCommandRegisteredDelegate(_logger, e.Command.CommandId, null);
        }
        // The wrapper will be created lazily when the command is first accessed
    }

    /// <summary>
    /// Handle command unregistration
    /// </summary>
    private void OnCommandUnregistered(object? sender, CommandUnregisteredEventArgs e)
    {
        if (_logger != null)
        {
            _logCommandUnregisteredDelegate(_logger, e.CommandId, null);
        }

        if (_commandWrappers.TryGetValue(e.CommandId, out var wrapper))
        {
            _commandWrappers.Remove(e.CommandId);
            // Note: The wrapper will be disposed when the command is disposed
        }
    }

    /// <summary>
    /// Refresh all command wrappers
    /// </summary>
    private void RefreshCommandWrappers()
    {
        _commandWrappers.Clear();

        foreach (var command in _commandRegistry.Commands)
        {
            GetOrCreateWrapper(command);
        }
    }

    /// <summary>
    /// Execute a command by ID
    /// </summary>
    public async void ExecuteCommand(string commandId, IDictionary<string, object>? parameters = null)
    {
        var command = _commandRegistry.GetCommand(commandId);
        if (command == null)
        {
            if (_logger != null)
            {
                _logCommandNotFoundDelegate(_logger, commandId, null);
            }
            return;
        }

        var wrapper = GetOrCreateWrapper(command);

        // Set parameters if provided
        if (parameters != null)
        {
            // Create a new wrapper with parameters for this execution
            var context = CreateCommandContext();
            var parameterizedWrapper = new PluginCommandWrapper(command, context, parameters, _logger);

            parameterizedWrapper.ExecutionStarted += (s, e) => CommandExecutionStarted?.Invoke(this, e);
            parameterizedWrapper.ExecutionCompleted += (s, e) => CommandExecutionCompleted?.Invoke(this, e);
            parameterizedWrapper.ProgressReported += (s, e) => CommandProgressReported?.Invoke(this, e);

            await parameterizedWrapper.ExecuteAsync().ConfigureAwait(false);
        }
        else
        {
            await wrapper.ExecuteAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Get command information for UI display
    /// </summary>
    public CommandDisplayInfo GetCommandDisplayInfo(string commandId)
    {
        var command = _commandRegistry.GetCommand(commandId);
        if (command == null)
            return new CommandDisplayInfo { CommandId = commandId, DisplayName = "Unknown Command" };

        return new CommandDisplayInfo
        {
            CommandId = command.CommandId,
            DisplayName = command.DisplayName,
            Description = command.Description,
            IconResource = command.IconResource,
            KeyboardShortcut = command.KeyboardShortcut,
            Category = command.Category,
            IsEnabled = command.IsEnabled,
            IsVisible = command.IsVisible
        };
    }
}

/// <summary>
/// Command display information for UI binding
/// </summary>
public class CommandDisplayInfo
{
    public string CommandId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? IconResource { get; init; }
    public string? KeyboardShortcut { get; init; }
    public CommandCategory Category { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsVisible { get; init; }
}
