using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ArtStudio.Core;

/// <summary>
/// Interface for plugin commands that can be executed in both UI and CLI contexts
/// </summary>
public interface IPluginCommand : IDisposable
{
    /// <summary>
    /// Unique identifier for the command (used for CLI and automation)
    /// </summary>
    string CommandId { get; }

    /// <summary>
    /// Display name for UI contexts
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Short description for tooltips
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Detailed instructions for help systems
    /// </summary>
    string? DetailedInstructions { get; }

    /// <summary>
    /// Icon resource path or identifier for UI
    /// </summary>
    string? IconResource { get; }

    /// <summary>
    /// Keyboard shortcut as string (e.g., "Ctrl+S")
    /// </summary>
    string? KeyboardShortcut { get; }

    /// <summary>
    /// Command category for organization
    /// </summary>
    CommandCategory Category { get; }

    /// <summary>
    /// Priority for ordering in menus (lower numbers = higher priority)
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Whether the command is currently enabled
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Whether the command is currently visible
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Command parameters schema for CLI and automation
    /// </summary>
    IReadOnlyDictionary<string, CommandParameter>? Parameters { get; }

    /// <summary>
    /// Event fired when the command's state changes
    /// </summary>
    event EventHandler? StateChanged;

    /// <summary>
    /// Prepare the command for execution (called before CanExecute/Execute)
    /// </summary>
    /// <param name="context">Command execution context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PrepareAsync(ICommandContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the command can be executed in the current context
    /// </summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>True if the command can be executed</returns>
    bool CanExecute(ICommandContext context, IDictionary<string, object>? parameters = null);

    /// <summary>
    /// Execute the command
    /// </summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Command execution result</returns>
    Task<CommandResult> ExecuteAsync(ICommandContext context, IDictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleanup after command execution (called after Execute regardless of success/failure)
    /// </summary>
    /// <param name="context">Command execution context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CleanupAsync(ICommandContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Command execution context
/// </summary>
public interface ICommandContext
{
    /// <summary>
    /// Service provider for dependency injection
    /// </summary>
    IServiceProvider ServiceProvider { get; }


    /// <summary>
    /// Configuration manager
    /// </summary>
    IConfigurationManager ConfigurationManager { get; }

    /// <summary>
    /// Execution mode (UI or CLI)
    /// </summary>
    CommandExecutionMode ExecutionMode { get; }

    /// <summary>
    /// Progress reporter for long-running operations
    /// </summary>
    IProgress<CommandProgress>? Progress { get; }

    /// <summary>
    /// Additional context data
    /// </summary>
    IDictionary<string, object> Data { get; }

}

/// <summary>
/// Command execution result
/// </summary>
public class CommandResult
{
    /// <summary>
    /// Whether the command executed successfully
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Result message or error description
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Exception if the command failed
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Additional result data
    /// </summary>
    public IDictionary<string, object>? Data { get; init; }

    /// <summary>
    /// Create a successful result
    /// </summary>
    public static CommandResult Success(string? message = null, IDictionary<string, object>? data = null)
        => new() { IsSuccess = true, Message = message, Data = data };

    /// <summary>
    /// Create a failed result
    /// </summary>
    public static CommandResult Failure(string message, Exception? exception = null)
        => new() { IsSuccess = false, Message = message, Exception = exception };
}

/// <summary>
/// Command progress information
/// </summary>
public class CommandProgress
{
    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int Percentage { get; init; }

    /// <summary>
    /// Current operation description
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Whether the operation can be cancelled
    /// </summary>
    public bool IsCancellable { get; init; }
}

/// <summary>
/// Command parameter definition
/// </summary>
public class CommandParameter
{
    /// <summary>
    /// Parameter name
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Parameter type
    /// </summary>
    public Type Type { get; init; } = typeof(object);

    /// <summary>
    /// Whether the parameter is required
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// Default value for optional parameters
    /// </summary>
    public object? DefaultValue { get; init; }

    /// <summary>
    /// Parameter description
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Valid values for enumeration parameters
    /// </summary>
    public IReadOnlyList<object>? ValidValues { get; init; }
}

/// <summary>
/// Command categories for organization
/// </summary>
public enum CommandCategory
{
    File,
    Edit,
    View,
    Image,
    Layer,
    Filter,
    Tool,
    Plugin,
    Help,
    Custom
}

/// <summary>
/// Command execution modes
/// </summary>
public enum CommandExecutionMode
{
    /// <summary>
    /// Interactive UI mode
    /// </summary>
    Interactive,

    /// <summary>
    /// Command-line interface mode
    /// </summary>
    CommandLine,

    /// <summary>
    /// Automated/scripted mode
    /// </summary>
    Automation
}
