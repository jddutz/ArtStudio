using System.CommandLine;
using System.Text.Json;
using ArtStudio.CLI.Models;
using ArtStudio.CLI.Services;
using Microsoft.Extensions.Logging;

namespace ArtStudio.CLI.Commands;

/// <summary>
/// Builds the batch command for running multiple commands from a file
/// </summary>
public class BatchCommandBuilder
{
    private readonly BatchProcessor _batchProcessor;
    private readonly ArgumentParser _argumentParser;
    private readonly OutputFormatter _outputFormatter;
    private readonly ILogger<BatchCommandBuilder> _logger;

    /// <summary>
    /// Initialize the batch command builder
    /// </summary>
    public BatchCommandBuilder(
        BatchProcessor batchProcessor,
        ArgumentParser argumentParser,
        OutputFormatter outputFormatter,
        ILogger<BatchCommandBuilder> logger)
    {
        _batchProcessor = batchProcessor ?? throw new ArgumentNullException(nameof(batchProcessor));
        _argumentParser = argumentParser ?? throw new ArgumentNullException(nameof(argumentParser));
        _outputFormatter = outputFormatter ?? throw new ArgumentNullException(nameof(outputFormatter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Build the batch command
    /// </summary>
    public Command Build()
    {
        var batchCommand = new Command("batch", "Execute commands from a batch file");

        // Batch file argument
        var batchFileArgument = new Argument<string>(
            name: "batch-file",
            description: "Path to the JSON file containing command definitions");
        batchCommand.AddArgument(batchFileArgument);

        // Continue on error option
        var continueOnErrorOption = new Option<bool>(
            aliases: new[] { "--continue-on-error", "-c" },
            description: "Continue executing remaining commands if one fails");
        batchCommand.AddOption(continueOnErrorOption);

        // Parallel execution option
        var parallelOption = new Option<int>(
            aliases: new[] { "--parallel", "-p" },
            getDefaultValue: () => 1,
            description: "Number of commands to execute in parallel");
        batchCommand.AddOption(parallelOption);

        // Timeout option
        var timeoutOption = new Option<int?>(
            aliases: new[] { "--timeout", "-t" },
            description: "Global timeout for the entire batch in milliseconds");
        batchCommand.AddOption(timeoutOption);

        // Validate option
        var validateOption = new Option<bool>(
            aliases: new[] { "--validate", "-v" },
            getDefaultValue: () => true,
            description: "Validate commands before execution");
        batchCommand.AddOption(validateOption);

        // Log file option
        var logFileOption = new Option<string?>(
            aliases: new[] { "--log-file", "-l" },
            description: "Path to save execution log");
        batchCommand.AddOption(logFileOption);

        // Dry run option
        var dryRunOption = new Option<bool>(
            aliases: new[] { "--dry-run", "-n" },
            description: "Show what would be executed without actually running the commands");
        batchCommand.AddOption(dryRunOption);

        // Output format option
        var formatOption = new Option<string>(
            aliases: new[] { "--format", "-f" },
            getDefaultValue: () => "text",
            description: "Output format (text, json, yaml, table)");
        batchCommand.AddOption(formatOption);

        // Set handler
        batchCommand.SetHandler(async (batchFile, continueOnError, parallel, timeout, validate, logFile, dryRun, format) =>
        {
            try
            {
                if (!File.Exists(batchFile))
                {
                    Console.Error.WriteLine($"Error: Batch file not found: {batchFile}");
                    Environment.ExitCode = 1;
                    return;
                }

                var requests = await _argumentParser.ParseBatchFileAsync(batchFile);
                var requestList = requests.ToList();

                if (dryRun)
                {
                    Console.WriteLine($"Would execute {requestList.Count} commands from {batchFile}:");
                    foreach (var request in requestList)
                    {
                        Console.WriteLine($"  - {request.CommandId}");
                        if (request.Parameters?.Count > 0)
                        {
                            Console.WriteLine($"    Parameters: {string.Join(", ", request.Parameters.Keys)}");
                        }
                    }
                    return;
                }

                var options = new BatchOptions
                {
                    ContinueOnError = continueOnError,
                    MaxParallelism = parallel,
                    GlobalTimeoutMs = timeout,
                    ValidateBeforeExecution = validate,
                    CreateExecutionLog = !string.IsNullOrWhiteSpace(logFile),
                    ExecutionLogPath = logFile
                };

                using var cts = timeout.HasValue
                    ? new CancellationTokenSource(TimeSpan.FromMilliseconds(timeout.Value))
                    : new CancellationTokenSource();

                var result = await _batchProcessor.ExecuteAsync(requestList, options, cts.Token);

                var outputFormat = Enum.TryParse<OutputFormatter.OutputFormat>(format, true, out var fmt)
                    ? fmt
                    : OutputFormatter.OutputFormat.Text;

                var output = _outputFormatter.FormatBatchResult(result, outputFormat);
                Console.WriteLine(output);

                // Save execution log if requested
                if (!string.IsNullOrWhiteSpace(logFile))
                {
                    await SaveExecutionLogAsync(logFile, result);
                }

                Environment.ExitCode = result.IsSuccess ? 0 : 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing batch file {BatchFile}", batchFile);
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        },
        batchFileArgument,
        continueOnErrorOption,
        parallelOption,
        timeoutOption,
        validateOption,
        logFileOption,
        dryRunOption,
        formatOption);

        return batchCommand;
    }

    /// <summary>
    /// Save execution log to file
    /// </summary>
    private static async Task SaveExecutionLogAsync(string logFile, BatchResult result)
    {
        var logData = new
        {
            executedAt = DateTimeOffset.UtcNow,
            batchResult = new
            {
                success = result.IsSuccess,
                successCount = result.SuccessCount,
                failureCount = result.FailureCount,
                startedAt = result.StartedAt,
                completedAt = result.CompletedAt,
                duration = result.Duration,
                results = result.Results.Select(r => new
                {
                    commandId = r.CommandId,
                    success = r.Result.IsSuccess,
                    message = r.Result.Message,
                    executedAt = r.ExecutedAt,
                    duration = r.Duration,
                    data = r.Result.Data,
                    exception = r.Exception?.Message
                })
            }
        };

        var json = JsonSerializer.Serialize(logData, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(logFile, json);
    }
}
