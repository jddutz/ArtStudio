using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ArtStudio.Core;
using Microsoft.Extensions.Logging;

namespace ArtStudio.Core.Services;

/// <summary>
/// CLI command executor for automation scenarios
/// </summary>
[Obsolete("This class has been moved to ArtStudio.CLI project. Use the standalone CLI application instead.", false)]
public class CliCommandExecutor
{
    private readonly ICommandRegistry _commandRegistry;
    private readonly ICommandContext _defaultContext;
    private readonly ILogger<CliCommandExecutor>? _logger;

    // LoggerMessage delegates for high-performance logging
    private static readonly Action<ILogger, string, Exception?> LogExecutingCommand =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(2001, nameof(LogExecutingCommand)),
            "Executing CLI command: {CommandId}");

    private static readonly Action<ILogger, string, Exception?> LogCommandNotFound =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(2002, nameof(LogCommandNotFound)),
            "Command '{CommandId}' not found");

    private static readonly Action<ILogger, string, Exception?> LogCommandCannotExecute =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(2003, nameof(LogCommandCannotExecute)),
            "Command '{CommandId}' cannot be executed");

    private static readonly Action<ILogger, string, Exception?> LogCommandExecuteSuccess =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(2004, nameof(LogCommandExecuteSuccess)),
            "Command '{CommandId}' executed successfully");

    private static readonly Action<ILogger, string, string?, Exception?> LogCommandFailure =
        LoggerMessage.Define<string, string?>(LogLevel.Error, new EventId(2005, nameof(LogCommandFailure)),
            "CLI command {CommandId} failed: {Message}");

    private static readonly Action<ILogger, string, Exception?> LogCommandExecuteError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(2006, nameof(LogCommandExecuteError)),
            "Error executing CLI command {CommandId}");

    private static readonly Action<ILogger, int, Exception?> LogExecutingBatch =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(2007, nameof(LogExecutingBatch)),
            "Executing batch of {Count} commands");

    private static readonly Action<ILogger, string, Exception?> LogBatchCommandWarning =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(2008, nameof(LogBatchCommandWarning)),
            "Batch execution stopped due to failed command: {CommandId}");

    private static readonly Action<ILogger, string, Exception?> LogBatchCommandError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(2009, nameof(LogBatchCommandError)),
            "Batch execution stopped due to exception in command: {CommandId}");

    private static readonly Action<ILogger, int, int, Exception?> LogBatchCompleted =
        LoggerMessage.Define<int, int>(LogLevel.Information, new EventId(2010, nameof(LogBatchCompleted)),
            "Batch execution completed: {SuccessCount} successful, {FailureCount} failed");

    /// <summary>
    /// Initialize the CLI executor
    /// </summary>
    public CliCommandExecutor(
        ICommandRegistry commandRegistry,
        ICommandContext defaultContext,
        ILogger<CliCommandExecutor>? logger = null)
    {
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
        _defaultContext = defaultContext ?? throw new ArgumentNullException(nameof(defaultContext));
        _logger = logger;
    }

    /// <summary>
    /// Execute a command by ID with parameters
    /// </summary>
    public async Task<CommandResult> ExecuteCommandAsync(
        string commandId,
        IDictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_logger != null)
                LogExecutingCommand(_logger, commandId, null);

            var command = _commandRegistry.GetCommand(commandId);
            if (command == null)
            {
                if (_logger != null)
                    LogCommandNotFound(_logger, commandId, null);
                return CommandResult.Failure($"Command '{commandId}' not found");
            }

            // Create CLI context
            var context = CreateCliContext(_defaultContext);

            // Prepare command
            await command.PrepareAsync(context, cancellationToken).ConfigureAwait(false);

            // Check if command can execute
            if (!command.CanExecute(context, parameters))
            {
                if (_logger != null)
                    LogCommandCannotExecute(_logger, commandId, null);
                return CommandResult.Failure($"Command '{commandId}' cannot be executed in the current context");
            }

            // Execute command
            var result = await command.ExecuteAsync(context, parameters, cancellationToken).ConfigureAwait(false);

            // Cleanup
            await command.CleanupAsync(context, cancellationToken).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                if (_logger != null)
                    LogCommandExecuteSuccess(_logger, commandId, null);
            }
            else
            {
                if (_logger != null)
                    LogCommandFailure(_logger, commandId, result.Message, null);
            }

            return result;
        }
#pragma warning disable CA1031 // Do not catch general exception types
        // Suppress CA1031: This method is designed to gracefully handle all exceptions
        // and return appropriate error results instead of crashing the CLI interface.
        catch (Exception ex)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            // Suppress CA1848: Direct logging is preferred for exception handling in CLI interface
            // to ensure immediate feedback without the overhead of delegate compilation.
            _logger?.LogError(ex, "Error executing CLI command {CommandId}", commandId);
#pragma warning restore CA1848
            return CommandResult.Failure($"Error executing command: {ex.Message}", ex);
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Execute multiple commands in sequence
    /// </summary>
    public async Task<BatchCommandResult> ExecuteBatchAsync(
        IEnumerable<BatchCommandRequest> requests,
        bool continueOnError = false,
        CancellationToken cancellationToken = default)
    {
        var results = new List<(string CommandId, CommandResult Result)>();
        var requestList = requests.ToList();

#pragma warning disable CA1848 // Use the LoggerMessage delegates
        // Suppress CA1848: Direct logging is preferred for batch operations in CLI interface
        // to ensure immediate feedback without the overhead of delegate compilation.
        _logger?.LogInformation("Executing batch of {Count} commands", requestList.Count);
#pragma warning restore CA1848

        foreach (var request in requestList)
        {
            try
            {
                var result = await ExecuteCommandAsync(request.CommandId, request.Parameters, cancellationToken).ConfigureAwait(false);
                results.Add((request.CommandId, result));

                if (!result.IsSuccess && !continueOnError)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    // Suppress CA1848: Direct logging is preferred for batch operations in CLI interface
                    // to ensure immediate feedback without the overhead of delegate compilation.
                    _logger?.LogWarning("Batch execution stopped due to failed command: {CommandId}", request.CommandId);
#pragma warning restore CA1848
                    break;
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            // Suppress CA1031: This method processes batches and should handle all exceptions
            // gracefully, converting them to appropriate error results without stopping the batch.
            catch (Exception ex)
            {
                var failureResult = CommandResult.Failure($"Exception during batch execution: {ex.Message}", ex);
                results.Add((request.CommandId, failureResult));

                if (!continueOnError)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    // Suppress CA1848: Direct logging is preferred for exception handling in CLI interface
                    // to ensure immediate feedback without the overhead of delegate compilation.
                    _logger?.LogError(ex, "Batch execution stopped due to exception in command: {CommandId}", request.CommandId);
#pragma warning restore CA1848
                    break;
                }
            }
#pragma warning restore CA1031
        }

        var successCount = results.Count(r => r.Result.IsSuccess);
        var failureCount = results.Count - successCount;

#pragma warning disable CA1848 // Use the LoggerMessage delegates
        // Suppress CA1848: Direct logging is preferred for batch operations in CLI interface
        // to ensure immediate feedback without the overhead of delegate compilation.
        _logger?.LogInformation("Batch execution completed: {SuccessCount} successful, {FailureCount} failed",
            successCount, failureCount);
#pragma warning restore CA1848

        return new BatchCommandResult
        {
            Results = results,
            SuccessCount = successCount,
            FailureCount = failureCount,
            IsSuccess = failureCount == 0
        };
    }

    /// <summary>
    /// Parse command line arguments into command requests
    /// </summary>
    public static BatchCommandRequest ParseCommandLine(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length == 0)
            throw new ArgumentException("No command specified");

        var commandId = args[0];
        var parameters = new Dictionary<string, object>();

        for (int i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                var paramName = arg[2..];
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    var value = args[i + 1];
                    parameters[paramName] = ParseParameterValue(value);
                    i++; // Skip the value
                }
                else
                {
                    parameters[paramName] = true; // Boolean flag
                }
            }
        }

        return new BatchCommandRequest
        {
            CommandId = commandId,
            Parameters = parameters
        };
    }

    /// <summary>
    /// Parse parameter value from string
    /// </summary>
    private static object ParseParameterValue(string value)
    {
        // Try to parse as JSON first (for complex objects)
        try
        {
            return JsonSerializer.Deserialize<object>(value) ?? value;
        }
#pragma warning disable CA1031 // Do not catch general exception types  
        // Suppress CA1031: This method attempts JSON parsing and falls back to simple types.
        // We intentionally catch all exceptions from JSON parsing to provide graceful fallback.
        catch
        {
            // Fall back to simple type parsing
            if (bool.TryParse(value, out var boolValue))
                return boolValue;

            if (int.TryParse(value, out var intValue))
                return intValue;
#pragma warning restore CA1031

            if (double.TryParse(value, out var doubleValue))
                return doubleValue;

            return value; // Return as string
        }
    }

    /// <summary>
    /// Create CLI execution context
    /// </summary>
    private static CommandContext CreateCliContext(ICommandContext baseContext)
    {
        return new CommandContext(
            baseContext.ServiceProvider,
            baseContext.EditorService,
            baseContext.ConfigurationManager,
            CommandExecutionMode.CommandLine,
            null, // No progress reporting in CLI mode
            new Dictionary<string, object>(baseContext.Data)
        );
    }

    /// <summary>
    /// Get help information for a command
    /// </summary>
    public string GetCommandHelp(string commandId)
    {
        var command = _commandRegistry.GetCommand(commandId);
        if (command == null)
            return $"Command '{commandId}' not found";

        var help = $"Command: {command.CommandId}\n";
        help += $"Name: {command.DisplayName}\n";
        help += $"Description: {command.Description}\n";

        if (!string.IsNullOrWhiteSpace(command.DetailedInstructions))
            help += $"Instructions: {command.DetailedInstructions}\n";

        if (command.Parameters?.Count > 0)
        {
            help += "\nParameters:\n";
            foreach (var param in command.Parameters.Values)
            {
                help += $"  --{param.Name} ({param.Type.Name})";
                if (param.IsRequired)
                    help += " [required]";
                if (param.DefaultValue != null)
                    help += $" [default: {param.DefaultValue}]";
                help += "\n";

                if (!string.IsNullOrWhiteSpace(param.Description))
                    help += $"    {param.Description}\n";

                if (param.ValidValues?.Count > 0)
                    help += $"    Valid values: {string.Join(", ", param.ValidValues)}\n";
            }
        }

        return help;
    }

    /// <summary>
    /// List all available commands
    /// </summary>
    public string ListCommands()
    {
        var commands = _commandRegistry.Commands.ToList();
        if (commands.Count == 0)
            return "No commands available";

        var help = "Available commands:\n\n";

        foreach (var category in Enum.GetValues<CommandCategory>())
        {
            var categoryCommands = commands.Where(c => c.Category == category).ToList();
            if (categoryCommands.Count == 0)
                continue;

            help += $"{category}:\n";
            foreach (var command in categoryCommands.OrderBy(c => c.Priority).ThenBy(c => c.DisplayName))
            {
                help += $"  {command.CommandId} - {command.Description}";
                if (!string.IsNullOrWhiteSpace(command.KeyboardShortcut))
                    help += $" ({command.KeyboardShortcut})";
                help += "\n";
            }
            help += "\n";
        }

        help += "Use --help <command-id> for detailed help on a specific command.";
        return help;
    }
}

/// <summary>
/// Request for batch command execution
/// </summary>
public class BatchCommandRequest
{
    public string CommandId { get; init; } = string.Empty;
    public IDictionary<string, object>? Parameters { get; init; }
}

/// <summary>
/// Result of batch command execution
/// </summary>
public class BatchCommandResult
{
    public IReadOnlyList<(string CommandId, CommandResult Result)> Results { get; init; } = new List<(string, CommandResult)>();
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public bool IsSuccess { get; init; }
}
