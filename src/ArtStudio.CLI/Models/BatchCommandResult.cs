using ArtStudio.Core;

namespace ArtStudio.CLI.Models;

/// <summary>
/// Result of executing a single command in a batch
/// </summary>
public class BatchCommandResult
{
    /// <summary>
    /// The command ID that was executed
    /// </summary>
    public string CommandId { get; init; } = string.Empty;

    /// <summary>
    /// The result of the command execution
    /// </summary>
    public CommandResult Result { get; init; } = CommandResult.Failure("Not executed");

    /// <summary>
    /// When the command was executed
    /// </summary>
    public DateTimeOffset ExecutedAt { get; init; }

    /// <summary>
    /// Exception that occurred during execution, if any
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Duration of the command execution
    /// </summary>
    public TimeSpan? Duration { get; init; }
}
