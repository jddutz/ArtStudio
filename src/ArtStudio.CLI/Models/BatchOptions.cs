namespace ArtStudio.CLI.Models;

/// <summary>
/// Options for batch command execution
/// </summary>
public class BatchOptions
{
    /// <summary>
    /// Whether to continue executing remaining commands if one fails
    /// </summary>
    public bool ContinueOnError { get; init; } = false;

    /// <summary>
    /// Maximum number of parallel commands to execute (1 = sequential)
    /// </summary>
    public int MaxParallelism { get; init; } = 1;

    /// <summary>
    /// Global timeout for the entire batch in milliseconds
    /// </summary>
    public int? GlobalTimeoutMs { get; init; }

    /// <summary>
    /// Default timeout for individual commands in milliseconds
    /// </summary>
    public int? DefaultCommandTimeoutMs { get; init; }

    /// <summary>
    /// Whether to validate commands before execution
    /// </summary>
    public bool ValidateBeforeExecution { get; init; } = true;

    /// <summary>
    /// Whether to create a detailed execution log
    /// </summary>
    public bool CreateExecutionLog { get; init; } = false;

    /// <summary>
    /// Path to save the execution log
    /// </summary>
    public string? ExecutionLogPath { get; init; }
}
