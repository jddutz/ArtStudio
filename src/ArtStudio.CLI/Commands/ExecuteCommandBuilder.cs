using System.CommandLine;
using ArtStudio.CLI.Services;
using Microsoft.Extensions.Logging;

namespace ArtStudio.CLI.Commands;

/// <summary>
/// Builds the execute command for running individual commands
/// </summary>
public class ExecuteCommandBuilder
{
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
            aliases: new[] { "--param", "-p" },
            description: "Command parameters in key=value format")
        {
            AllowMultipleArgumentsPerToken = true
        };
        executeCommand.AddOption(parametersOption);

        // Timeout option
        var timeoutOption = new Option<int?>(
            aliases: new[] { "--timeout", "-t" },
            description: "Timeout in milliseconds");
        executeCommand.AddOption(timeoutOption);

        // Dry run option
        var dryRunOption = new Option<bool>(
            aliases: new[] { "--dry-run", "-n" },
            description: "Show what would be executed without actually running the command");
        executeCommand.AddOption(dryRunOption);

        // Output format option
        var formatOption = new Option<string>(
            aliases: new[] { "--format", "-f" },
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

                var result = await _commandExecutor.ExecuteAsync(commandId, parsedParams, cts.Token);

                var outputFormat = Enum.TryParse<OutputFormatter.OutputFormat>(format, true, out var fmt)
                    ? fmt
                    : OutputFormatter.OutputFormat.Text;

                var output = _outputFormatter.FormatCommandResult(result, outputFormat);
                Console.WriteLine(output);

                Environment.ExitCode = result.IsSuccess ? 0 : 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing command {CommandId}", commandId);
                Console.Error.WriteLine($"Error: {ex.Message}");
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
            var equalIndex = param.IndexOf('=');
            if (equalIndex <= 0 || equalIndex == param.Length - 1)
            {
                _logger.LogWarning("Invalid parameter format: {Parameter}. Expected key=value", param);
                continue;
            }

            var key = param[..equalIndex];
            var value = param[(equalIndex + 1)..];

            result[key] = _argumentParser.ParseParameterValue(value);
        }

        return result;
    }
}
