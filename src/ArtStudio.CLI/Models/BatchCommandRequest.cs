using System.Collections.Generic;

namespace ArtStudio.CLI.Models;

/// <summary>
/// Request for executing a command in a batch
/// </summary>
public class BatchCommandRequest
{
    /// <summary>
    /// The command ID to execute
    /// </summary>
    public string CommandId { get; init; } = string.Empty;

    /// <summary>
    /// Parameters to pass to the command
    /// </summary>
    public IDictionary<string, object>? Parameters { get; init; }

    /// <summary>
    /// Optional description for this command in the batch
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Whether to continue batch execution if this command fails
    /// </summary>
    public bool ContinueOnError { get; init; }

    /// <summary>
    /// Timeout for this command in milliseconds
    /// </summary>
    public int? TimeoutMs { get; init; }
}
