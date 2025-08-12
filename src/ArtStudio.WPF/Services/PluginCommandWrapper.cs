using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ArtStudio.Core;
using Microsoft.Extensions.Logging;

namespace ArtStudio.WPF.Services;

/// <summary>
/// WPF ICommand wrapper for IPluginCommand that provides error handling, logging, and progress reporting
/// </summary>
public class PluginCommandWrapper : ICommand
{
    private readonly IPluginCommand _pluginCommand;
    private readonly ICommandContext _context;
    private readonly ILogger? _logger;
    private readonly IDictionary<string, object>? _parameters;
    private bool _isExecuting;

    // High-performance logging delegates
    private static readonly Action<ILogger, string, Exception?> _logCanExecuteErrorDelegate =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(1, nameof(PluginCommandWrapper)), "Error checking CanExecute for command {CommandId}");

    private static readonly Action<ILogger, string, Exception?> _logCommandCancelledDelegate =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(2, nameof(PluginCommandWrapper)), "Command {CommandId} was cancelled");

    private static readonly Action<ILogger, string, Exception?> _logUnhandledErrorDelegate =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(3, nameof(PluginCommandWrapper)), "Unhandled error executing command {CommandId}");

    /// <summary>
    /// Event fired when CanExecute status changes
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// Event fired when command execution starts
    /// </summary>
    public event EventHandler<CommandExecutionEventArgs>? ExecutionStarted;

    /// <summary>
    /// Event fired when command execution completes
    /// </summary>
    public event EventHandler<CommandExecutionCompletedEventArgs>? ExecutionCompleted;

    /// <summary>
    /// Event fired during command execution to report progress
    /// </summary>
    public event EventHandler<CommandProgressEventArgs>? ProgressReported;

    /// <summary>
    /// The wrapped plugin command
    /// </summary>
    public IPluginCommand PluginCommand => _pluginCommand;

    /// <summary>
    /// Whether the command is currently executing
    /// </summary>
    public bool IsExecuting => _isExecuting;

    /// <summary>
    /// Initialize the wrapper
    /// </summary>
    public PluginCommandWrapper(
        IPluginCommand pluginCommand,
        ICommandContext context,
        IDictionary<string, object>? parameters = null,
        ILogger? logger = null)
    {
        _pluginCommand = pluginCommand ?? throw new ArgumentNullException(nameof(pluginCommand));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _parameters = parameters;
        _logger = logger;

        // Subscribe to plugin command state changes
        _pluginCommand.StateChanged += OnPluginCommandStateChanged;
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
    {
        try
        {
            if (_isExecuting)
                return false;

            return _pluginCommand.CanExecute(_context, _parameters);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        // Gracefully handle plugin errors to prevent UI crashes
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            if (_logger != null)
            {
                _logCanExecuteErrorDelegate(_logger, _pluginCommand.CommandId, ex);
            }
            return false;
        }
    }

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
            return;

        // Execute asynchronously without blocking the UI
        _ = ExecuteAsync();
    }

    /// <summary>
    /// Execute the command asynchronously
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_isExecuting || !CanExecute(null))
            return;

        _isExecuting = true;
        var startTime = DateTime.UtcNow;

        try
        {
            // Notify execution started
            OnExecutionStarted();
            RaiseCanExecuteChanged();

            // Create progress context
            var progressContext = CreateProgressContext();

            // Prepare the command
            await _pluginCommand.PrepareAsync(progressContext, cancellationToken).ConfigureAwait(false);

            // Execute the command
            var result = await _pluginCommand.ExecuteAsync(progressContext, _parameters, cancellationToken).ConfigureAwait(false);

            // Cleanup
            await _pluginCommand.CleanupAsync(progressContext, cancellationToken).ConfigureAwait(false);

            // Notify completion
            var duration = DateTime.UtcNow - startTime;
            OnExecutionCompleted(result, duration);
        }
        catch (OperationCanceledException)
        {
            if (_logger != null)
            {
                _logCommandCancelledDelegate(_logger, _pluginCommand.CommandId, null);
            }
            var duration = DateTime.UtcNow - startTime;
            OnExecutionCompleted(CommandResult.Failure("Command was cancelled"), duration);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        // Gracefully handle plugin errors to prevent application crashes
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            if (_logger != null)
            {
                _logUnhandledErrorDelegate(_logger, _pluginCommand.CommandId, ex);
            }
            var duration = DateTime.UtcNow - startTime;
            OnExecutionCompleted(CommandResult.Failure($"Unhandled error: {ex.Message}", ex), duration);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// Create a command context with progress reporting
    /// </summary>
    private ProgressCommandContext CreateProgressContext()
    {
        var progress = new Progress<CommandProgress>(OnProgressReported);
        return new ProgressCommandContext(_context, progress);
    }

    /// <summary>
    /// Handle plugin command state changes
    /// </summary>
    private void OnPluginCommandStateChanged(object? sender, EventArgs e)
    {
        RaiseCanExecuteChanged();
    }

    /// <summary>
    /// Raise the CanExecuteChanged event
    /// </summary>
    private void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Raise the ExecutionStarted event
    /// </summary>
    private void OnExecutionStarted()
    {
        ExecutionStarted?.Invoke(this, new CommandExecutionEventArgs(_pluginCommand.CommandId));
    }

    /// <summary>
    /// Raise the ExecutionCompleted event
    /// </summary>
    private void OnExecutionCompleted(CommandResult result, TimeSpan duration)
    {
        ExecutionCompleted?.Invoke(this, new CommandExecutionCompletedEventArgs(
            _pluginCommand.CommandId, result, duration));
    }

    /// <summary>
    /// Handle progress reports
    /// </summary>
    private void OnProgressReported(CommandProgress progress)
    {
        ProgressReported?.Invoke(this, new CommandProgressEventArgs(_pluginCommand.CommandId, progress));
    }
}

/// <summary>
/// Command context wrapper that adds progress reporting
/// </summary>
internal sealed class ProgressCommandContext : ICommandContext
{
    private readonly ICommandContext _baseContext;

    public ProgressCommandContext(ICommandContext baseContext, IProgress<CommandProgress> progress)
    {
        _baseContext = baseContext;
        Progress = progress;
    }

    public IServiceProvider ServiceProvider => _baseContext.ServiceProvider;
    public IConfigurationManager ConfigurationManager => _baseContext.ConfigurationManager;
    public CommandExecutionMode ExecutionMode => _baseContext.ExecutionMode;
    public IProgress<CommandProgress>? Progress { get; }
    public IDictionary<string, object> Data => _baseContext.Data;
}

/// <summary>
/// Event arguments for command execution start
/// </summary>
public class CommandExecutionEventArgs : EventArgs
{
    public string CommandId { get; }

    public CommandExecutionEventArgs(string commandId)
    {
        CommandId = commandId;
    }
}

/// <summary>
/// Event arguments for command execution completion
/// </summary>
public class CommandExecutionCompletedEventArgs : EventArgs
{
    public string CommandId { get; }
    public CommandResult Result { get; }
    public TimeSpan Duration { get; }

    public CommandExecutionCompletedEventArgs(string commandId, CommandResult result, TimeSpan duration)
    {
        CommandId = commandId;
        Result = result;
        Duration = duration;
    }
}

/// <summary>
/// Event arguments for command progress reporting
/// </summary>
public class CommandProgressEventArgs : EventArgs
{
    public string CommandId { get; }
    public CommandProgress Progress { get; }

    public CommandProgressEventArgs(string commandId, CommandProgress progress)
    {
        CommandId = commandId;
        Progress = progress;
    }
}
