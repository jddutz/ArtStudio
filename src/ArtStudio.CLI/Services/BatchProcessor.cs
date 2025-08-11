using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArtStudio.CLI.Models;
using Microsoft.Extensions.Logging;

namespace ArtStudio.CLI.Services;

/// <summary>
/// Service for executing multiple commands in sequence
/// </summary>
public class BatchProcessor
{
    private readonly CommandExecutor _commandExecutor;
    private readonly ILogger<BatchProcessor> _logger;

    // LoggerMessage delegates for high-performance logging
    private static readonly Action<ILogger, int, Exception?> LogExecutingBatch =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(3101, nameof(LogExecutingBatch)),
            "Executing batch of {Count} commands");

    private static readonly Action<ILogger, string, Exception?> LogBatchCommandWarning =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(3102, nameof(LogBatchCommandWarning)),
            "Batch execution stopped due to failed command: {CommandId}");

    private static readonly Action<ILogger, string, Exception?> LogBatchCommandError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(3103, nameof(LogBatchCommandError)),
            "Batch execution stopped due to exception in command: {CommandId}");

    private static readonly Action<ILogger, int, int, Exception?> LogBatchCompleted =
        LoggerMessage.Define<int, int>(LogLevel.Information, new EventId(3104, nameof(LogBatchCompleted)),
            "Batch execution completed: {SuccessCount} successful, {FailureCount} failed");

    /// <summary>
    /// Initialize the batch processor
    /// </summary>
    public BatchProcessor(
        CommandExecutor commandExecutor,
        ILogger<BatchProcessor> logger)
    {
        _commandExecutor = commandExecutor ?? throw new ArgumentNullException(nameof(commandExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Execute multiple commands in sequence
    /// </summary>
    public async Task<BatchResult> ExecuteAsync(
        IEnumerable<BatchCommandRequest> requests,
        BatchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new BatchOptions();
        var results = new List<BatchCommandResult>();
        var requestList = requests.ToList();

        LogExecutingBatch(_logger, requestList.Count, null);

        foreach (var request in requestList)
        {
            try
            {
                var result = await _commandExecutor.ExecuteAsync(
                    request.CommandId,
                    request.Parameters,
                    cancellationToken).ConfigureAwait(false);

                var batchResult = new BatchCommandResult
                {
                    CommandId = request.CommandId,
                    Result = result,
                    ExecutedAt = DateTimeOffset.UtcNow
                };

                results.Add(batchResult);

                if (!result.IsSuccess && !options.ContinueOnError)
                {
                    LogBatchCommandWarning(_logger, request.CommandId, null);
                    break;
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            // Intentionally catching all exceptions during batch processing to record failures
            // and continue with remaining commands rather than aborting the entire batch
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                var failureResult = Core.CommandResult.Failure($"Exception during batch execution: {ex.Message}", ex);
                var batchResult = new BatchCommandResult
                {
                    CommandId = request.CommandId,
                    Result = failureResult,
                    ExecutedAt = DateTimeOffset.UtcNow,
                    Exception = ex
                };

                results.Add(batchResult);

                if (!options.ContinueOnError)
                {
                    LogBatchCommandError(_logger, request.CommandId, ex);
                    break;
                }
            }
        }

        var successCount = results.Count(r => r.Result.IsSuccess);
        var failureCount = results.Count - successCount;

        LogBatchCompleted(_logger, successCount, failureCount, null);

        return new BatchResult
        {
            Results = results,
            SuccessCount = successCount,
            FailureCount = failureCount,
            IsSuccess = failureCount == 0,
            StartedAt = DateTimeOffset.UtcNow.AddMilliseconds(-results.Count * 100), // Approximate
            CompletedAt = DateTimeOffset.UtcNow
        };
    }
}
