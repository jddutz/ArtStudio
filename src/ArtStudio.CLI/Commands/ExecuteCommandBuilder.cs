using System.CommandLine;
using ArtStudio.CLI.Services;
using Microsoft.Extensions.Logging;

namespace ArtStudio.CLI.Commands;

/// <summary>
/// Builds the execute command for running individual commands
/// </summary>
public class ExecuteCommandBuilder
{
    private static readonly string[] ParametersAliases = ["--param", "-p"];
    private static readonly string[] TimeoutAliases = ["--timeout", "-t"];
    private static readonly string[] DryRunAliases = ["--dry-run", "-n"];
    private static readonly string[] FormatAliases = ["--format", "-f"];

    // LoggerMessage delegates for high-performance logging
    private static readonly Action<ILogger, string, Exception?> LogExecuteCommandError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(3001, nameof(LogExecuteCommandError)),
            "Error executing command {CommandId}");

    private static readonly Action<ILogger, string, Exception?> LogInvalidParameterFormat =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(3002, nameof(LogInvalidParameterFormat)),
            "Invalid parameter format: {Parameter}. Expected key=value");

    private readonly CommandExecutor _commandExecutor;
    private readonly ArgumentParser _argumentParser;
    private readonly OutputFormatter _outputFormatter;
    private readonly ILogger<ExecuteCommandBuilder> _logger;

    /// <summary>
    /// Initialize the execute command builder
    /// </summary>
    public ExecuteCommandBuilder(
        CommandExecutor commandExecutor,
        ArgumentParser argumentParser,
        OutputFormatter outputFormatter,
        ILogger<ExecuteCommandBuilder> logger)
    {
        _commandExecutor = commandExecutor ?? throw new ArgumentNullException(nameof(commandExecutor));
        _argumentParser = argumentParser ?? throw new ArgumentNullException(nameof(argumentParser));
        _outputFormatter = outputFormatter ?? throw new ArgumentNullException(nameof(outputFormatter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Build the execute command
    /// </summary>
    public Command Build()
    {
        var executeCommand = new Command("execute", "Execute a specific command");

        // Command ID argument
        var commandIdArgument = new Argument<string>(
            name: "command-id",
            description: "The ID of the command to execute");
        executeCommand.AddArgument(commandIdArgument);

        // Parameters option
        var parametersOption = new Option<string[]>(
            aliases: ParametersAliases,
            description: "Command parameters in key=value format")
        {
            AllowMultipleArgumentsPerToken = true
        };
        executeCommand.AddOption(parametersOption);

        // Timeout option
        var timeoutOption = new Option<int?>(
            aliases: TimeoutAliases,
            description: "Timeout in milliseconds");
        executeCommand.AddOption(timeoutOption);

        // Dry run option
        var dryRunOption = new Option<bool>(
            aliases: DryRunAliases,
            description: "Show what would be executed without actually running the command");
        executeCommand.AddOption(dryRunOption);

        // Output format option
        var formatOption = new Option<string>(
            aliases: FormatAliases,
            getDefaultValue: () => "text",
            description: "Output format (text, json, yaml, table)");
        executeCommand.AddOption(formatOption);

        // Set handler
        executeCommand.SetHandler(async (commandId, parameters, timeout, dryRun, format) =>
        {
            try
            {
                var parsedParams = ParseParameters(parameters);

                if (dryRun)
                {
                    Console.WriteLine($"Would execute command: {commandId}");
                    if (parsedParams.Count > 0)
                    {
                        Console.WriteLine("With parameters:");
                        foreach (var (key, value) in parsedParams)
                        {
                            Console.WriteLine($"  {key}: {value}");
                        }
                    }
                    return;
                }

                using var cts = timeout.HasValue
                    ? new CancellationTokenSource(TimeSpan.FromMilliseconds(timeout.Value))
                    : new CancellationTokenSource();

                var result = await _commandExecutor.ExecuteAsync(commandId, parsedParams, cts.Token).ConfigureAwait(false);

                var outputFormat = Enum.TryParse<OutputFormatter.OutputFormat>(format, true, out var fmt)
                    ? fmt
                    : OutputFormatter.OutputFormat.Text;

                var output = _outputFormatter.FormatCommandResult(result, outputFormat);
                Console.WriteLine(output);

                Environment.ExitCode = result.IsSuccess ? 0 : 1;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            // Intentionally catching all exceptions in CLI command handler to provide
            // user-friendly error messages and appropriate exit codes
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                LogExecuteCommandError(_logger, commandId, ex);
#pragma warning disable CA1849 // Call async methods when in an async method
                // Intentionally using synchronous Console.Error.WriteLine for immediate error feedback in CLI
                Console.Error.WriteLine($"Error: {ex.Message}");
#pragma warning restore CA1849 // Call async methods when in an async method
                Environment.ExitCode = 1;
            }
        },
        commandIdArgument,
        parametersOption,
        timeoutOption,
        dryRunOption,
        formatOption);

        return executeCommand;
    }

    /// <summary>
    /// Parse parameter strings into dictionary
    /// </summary>
    private Dictionary<string, object> ParseParameters(string[] parameters)
    {
        var result = new Dictionary<string, object>();

        foreach (var param in parameters)
        {
            var equalIndex = param.IndexOf('=', StringComparison.Ordinal);
            if (equalIndex <= 0 || equalIndex == param.Length - 1)
            {
                LogInvalidParameterFormat(_logger, param, null);
                continue;
            }

            var key = param[..equalIndex];
            var value = param[(equalIndex + 1)..];

            result[key] = _argumentParser.ParseParameterValue(value);
        }

        return result;
    }
}
