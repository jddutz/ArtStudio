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
public class CliCommandExecutor
{
    private readonly ICommandRegistry _commandRegistry;
    private readonly ICommandContext _defaultContext;
    private readonly ILogger<CliCommandExecutor>? _logger;

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
            _logger?.LogInformation("Executing CLI command: {CommandId}", commandId);

            var command = _commandRegistry.GetCommand(commandId);
            if (command == null)
            {
                var message = $"Command '{commandId}' not found";
                _logger?.LogError(message);
                return CommandResult.Failure(message);
            }

            // Create CLI context
            var context = CreateCliContext(_defaultContext);

            // Prepare command
            await command.PrepareAsync(context, cancellationToken);

            // Check if command can execute
            if (!command.CanExecute(context, parameters))
            {
                var message = $"Command '{commandId}' cannot be executed in the current context";
                _logger?.LogWarning(message);
                return CommandResult.Failure(message);
            }

            // Execute command
            var result = await command.ExecuteAsync(context, parameters, cancellationToken);

            // Cleanup
            await command.CleanupAsync(context, cancellationToken);

            if (result.IsSuccess)
            {
                _logger?.LogInformation("CLI command {CommandId} executed successfully", commandId);
            }
            else
            {
                _logger?.LogError("CLI command {CommandId} failed: {Message}", commandId, result.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing CLI command {CommandId}", commandId);
            return CommandResult.Failure($"Error executing command: {ex.Message}", ex);
        }
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

        _logger?.LogInformation("Executing batch of {Count} commands", requestList.Count);

        foreach (var request in requestList)
        {
            try
            {
                var result = await ExecuteCommandAsync(request.CommandId, request.Parameters, cancellationToken);
                results.Add((request.CommandId, result));

                if (!result.IsSuccess && !continueOnError)
                {
                    _logger?.LogWarning("Batch execution stopped due to failed command: {CommandId}", request.CommandId);
                    break;
                }
            }
            catch (Exception ex)
            {
                var failureResult = CommandResult.Failure($"Exception during batch execution: {ex.Message}", ex);
                results.Add((request.CommandId, failureResult));

                if (!continueOnError)
                {
                    _logger?.LogError(ex, "Batch execution stopped due to exception in command: {CommandId}", request.CommandId);
                    break;
                }
            }
        }

        var successCount = results.Count(r => r.Result.IsSuccess);
        var failureCount = results.Count - successCount;

        _logger?.LogInformation("Batch execution completed: {SuccessCount} successful, {FailureCount} failed",
            successCount, failureCount);

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
        if (args.Length == 0)
            throw new ArgumentException("No command specified");

        var commandId = args[0];
        var parameters = new Dictionary<string, object>();

        for (int i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith("--"))
            {
                var paramName = arg[2..];
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
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
        catch
        {
            // Fall back to simple type parsing
            if (bool.TryParse(value, out var boolValue))
                return boolValue;

            if (int.TryParse(value, out var intValue))
                return intValue;

            if (double.TryParse(value, out var doubleValue))
                return doubleValue;

            return value; // Return as string
        }
    }

    /// <summary>
    /// Create CLI execution context
    /// </summary>
    private static ICommandContext CreateCliContext(ICommandContext baseContext)
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
        if (!commands.Any())
            return "No commands available";

        var help = "Available commands:\n\n";

        foreach (var category in Enum.GetValues<CommandCategory>())
        {
            var categoryCommands = commands.Where(c => c.Category == category).ToList();
            if (!categoryCommands.Any())
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
