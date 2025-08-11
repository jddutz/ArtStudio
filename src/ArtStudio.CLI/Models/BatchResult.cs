using System.Collections.Generic;

namespace ArtStudio.CLI.Models;

/// <summary>
/// Result of executing a batch of commands
/// </summary>
public class BatchResult
{
    /// <summary>
    /// Results for each command in the batch
    /// </summary>
    public IReadOnlyList<BatchCommandResult> Results { get; init; } = new List<BatchCommandResult>();

    /// <summary>
    /// Number of successfully executed commands
    /// </summary>
    public int SuccessCount { get; init; }

    /// <summary>
    /// Number of failed commands
    /// </summary>
    public int FailureCount { get; init; }

    /// <summary>
    /// Whether the entire batch was successful
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// When the batch execution started
    /// </summary>
    public DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// When the batch execution completed
    /// </summary>
    public DateTimeOffset CompletedAt { get; init; }

    /// <summary>
    /// Total duration of the batch execution
    /// </summary>
    public TimeSpan Duration => CompletedAt - StartedAt;
}
