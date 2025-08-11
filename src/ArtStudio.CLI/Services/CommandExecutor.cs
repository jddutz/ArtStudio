using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArtStudio.Core;
using ArtStudio.Core.Services;
using Microsoft.Extensions.Logging;

namespace ArtStudio.CLI.Services;

/// <summary>
/// Service for executing individual commands in CLI context
/// </summary>
public class CommandExecutor
{
    private readonly ICommandRegistry _commandRegistry;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEditorService _editorService;
    private readonly IConfigurationManager _configurationManager;
    private readonly ILogger<CommandExecutor> _logger;

    // LoggerMessage delegates for high-performance logging
    private static readonly Action<ILogger, string, Exception?> LogExecutingCommand =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(3001, nameof(LogExecutingCommand)),
            "Executing command: {CommandId}");

    private static readonly Action<ILogger, string, Exception?> LogCommandNotFound =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(3002, nameof(LogCommandNotFound)),
            "Command '{CommandId}' not found");

    private static readonly Action<ILogger, string, Exception?> LogCommandCannotExecute =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(3003, nameof(LogCommandCannotExecute)),
            "Command '{CommandId}' cannot be executed");

    private static readonly Action<ILogger, string, Exception?> LogCommandSuccess =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(3004, nameof(LogCommandSuccess)),
            "Command '{CommandId}' executed successfully");

    private static readonly Action<ILogger, string, string?, Exception?> LogCommandFailure =
        LoggerMessage.Define<string, string?>(LogLevel.Error, new EventId(3005, nameof(LogCommandFailure)),
            "Command '{CommandId}' failed: {Message}");

    private static readonly Action<ILogger, string, Exception?> LogCommandExecutionError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(3006, nameof(LogCommandExecutionError)),
            "Error executing command {CommandId}");

    /// <summary>
    /// Initialize the command executor
    /// </summary>
    public CommandExecutor(
        ICommandRegistry commandRegistry,
        IServiceProvider serviceProvider,
        IEditorService editorService,
        IConfigurationManager configurationManager,
        ILogger<CommandExecutor> logger)
    {
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
        _configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Execute a command by ID with parameters
    /// </summary>
    public async Task<CommandResult> ExecuteAsync(
        string commandId,
        IDictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            LogExecutingCommand(_logger, commandId, null);

            var command = _commandRegistry.GetCommand(commandId);
            if (command == null)
            {
                LogCommandNotFound(_logger, commandId, null);
                return CommandResult.Failure($"Command '{commandId}' not found");
            }

            // Create CLI context
            var context = CreateCliContext();

            // Prepare command
            await command.PrepareAsync(context, cancellationToken).ConfigureAwait(false);

            // Check if command can execute
            if (!command.CanExecute(context, parameters))
            {
                LogCommandCannotExecute(_logger, commandId, null);
                return CommandResult.Failure($"Command '{commandId}' cannot be executed in the current context");
            }

            // Execute command
            var result = await command.ExecuteAsync(context, parameters, cancellationToken).ConfigureAwait(false);

            // Cleanup
            await command.CleanupAsync(context, cancellationToken).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                LogCommandSuccess(_logger, commandId, null);
            }
            else
            {
                LogCommandFailure(_logger, commandId, result.Message, null);
            }

            return result;
        }
#pragma warning disable CA1031 // Do not catch general exception types
        // Intentionally catching all exceptions to return structured CommandResult.Failure
        // instead of allowing exceptions to propagate and crash the CLI
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            LogCommandExecutionError(_logger, commandId, ex);
            return CommandResult.Failure($"Error executing command: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Create CLI execution context
    /// </summary>
    private CommandContext CreateCliContext()
    {
        return new CommandContext(
            _serviceProvider,
            _editorService,
            _configurationManager,
            CommandExecutionMode.CommandLine,
            null, // No progress reporting in CLI mode
            new Dictionary<string, object>()
        );
    }
}
