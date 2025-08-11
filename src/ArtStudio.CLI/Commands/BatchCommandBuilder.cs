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
    private static readonly string[] ContinueOnErrorAliases = ["--continue-on-error", "-c"];
    private static readonly string[] ParallelAliases = ["--parallel", "-p"];
    private static readonly string[] TimeoutAliases = ["--timeout", "-t"];
    private static readonly string[] ValidateAliases = ["--validate", "-v"];
    private static readonly string[] LogFileAliases = ["--log-file", "-l"];
    private static readonly string[] DryRunAliases = ["--dry-run", "-n"];
    private static readonly string[] FormatAliases = ["--format", "-f"];

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // LoggerMessage delegates for high-performance logging
    private static readonly Action<ILogger, string, Exception?> LogBatchExecutionError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(4001, nameof(LogBatchExecutionError)),
            "Error executing batch file {BatchFile}");

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
            aliases: ContinueOnErrorAliases,
            description: "Continue executing remaining commands if one fails");
        batchCommand.AddOption(continueOnErrorOption);

        // Parallel execution option
        var parallelOption = new Option<int>(
            aliases: ParallelAliases,
            getDefaultValue: () => 1,
            description: "Number of commands to execute in parallel");
        batchCommand.AddOption(parallelOption);

        // Timeout option
        var timeoutOption = new Option<int?>(
            aliases: TimeoutAliases,
            description: "Global timeout for the entire batch in milliseconds");
        batchCommand.AddOption(timeoutOption);

        // Validate option
        var validateOption = new Option<bool>(
            aliases: ValidateAliases,
            getDefaultValue: () => true,
            description: "Validate commands before execution");
        batchCommand.AddOption(validateOption);

        // Log file option
        var logFileOption = new Option<string?>(
            aliases: LogFileAliases,
            description: "Path to save execution log");
        batchCommand.AddOption(logFileOption);

        // Dry run option
        var dryRunOption = new Option<bool>(
            aliases: DryRunAliases,
            description: "Show what would be executed without actually running the commands");
        batchCommand.AddOption(dryRunOption);

        // Output format option
        var formatOption = new Option<string>(
            aliases: FormatAliases,
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
#pragma warning disable CA1849 // Call async methods when in an async method
                    // Intentionally using synchronous Console.Error.WriteLine for immediate error feedback in CLI
                    Console.Error.WriteLine($"Error: Batch file not found: {batchFile}");
#pragma warning restore CA1849 // Call async methods when in an async method
                    Environment.ExitCode = 1;
                    return;
                }

                var requests = await _argumentParser.ParseBatchFileAsync(batchFile).ConfigureAwait(false);
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

                var result = await _batchProcessor.ExecuteAsync(requestList, options, cts.Token).ConfigureAwait(false);

                var outputFormat = Enum.TryParse<OutputFormatter.OutputFormat>(format, true, out var fmt)
                    ? fmt
                    : OutputFormatter.OutputFormat.Text;

                var output = _outputFormatter.FormatBatchResult(result, outputFormat);
                Console.WriteLine(output);

                // Save execution log if requested
                if (!string.IsNullOrWhiteSpace(logFile))
                {
                    await SaveExecutionLogAsync(logFile, result).ConfigureAwait(false);
                }

                Environment.ExitCode = result.IsSuccess ? 0 : 1;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            // Intentionally catching all exceptions in CLI command handler to provide
            // user-friendly error messages and appropriate exit codes
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                LogBatchExecutionError(_logger, batchFile, ex);
#pragma warning disable CA1849 // Call async methods when in an async method
                // Intentionally using synchronous Console.Error.WriteLine for immediate error feedback in CLI
                Console.Error.WriteLine($"Error: {ex.Message}");
#pragma warning restore CA1849 // Call async methods when in an async method
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

        var json = JsonSerializer.Serialize(logData, JsonOptions);
        await File.WriteAllTextAsync(logFile, json).ConfigureAwait(false);
    }
}
