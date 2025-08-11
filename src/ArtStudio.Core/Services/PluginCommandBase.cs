using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArtStudio.Core;
using Microsoft.Extensions.Logging;

namespace ArtStudio.Core.Services;

/// <summary>
/// Base implementation for plugin commands
/// </summary>
public abstract class PluginCommandBase : IPluginCommand
{
    private readonly ILogger? _logger;
    private bool _disposed;

    protected ILogger? Logger => _logger;

    // LoggerMessage delegates for high-performance logging
    private static readonly Action<ILogger, string, Exception?> LogPreparingCommand =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1001, nameof(LogPreparingCommand)),
            "Preparing command {CommandId}");

    private static readonly Action<ILogger, string, string, Exception?> LogMissingParameter =
        LoggerMessage.Define<string, string>(LogLevel.Warning, new EventId(1002, nameof(LogMissingParameter)),
            "Required parameter {ParameterName} is missing for command {CommandId}");

    private static readonly Action<ILogger, string, Exception?> LogExecutingCommand =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1003, nameof(LogExecutingCommand)),
            "Executing command {CommandId}");

    private static readonly Action<ILogger, string, Exception?> LogCommandSuccess =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1004, nameof(LogCommandSuccess)),
            "Command {CommandId} executed successfully");

    private static readonly Action<ILogger, string, string?, Exception?> LogCommandFailure =
        LoggerMessage.Define<string, string?>(LogLevel.Warning, new EventId(1005, nameof(LogCommandFailure)),
            "Command {CommandId} failed: {Message}");

    private static readonly Action<ILogger, string, Exception?> LogCommandCancelled =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1006, nameof(LogCommandCancelled)),
            "Command {CommandId} was cancelled");

    private static readonly Action<ILogger, string, Exception?> LogCommandError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(1007, nameof(LogCommandError)),
            "Unexpected error executing command {CommandId}");

    private static readonly Action<ILogger, string, Exception?> LogCleaningUpCommand =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1008, nameof(LogCleaningUpCommand)),
            "Cleaning up command {CommandId}");

    /// <summary>
    /// Initialize the command with optional logger
    /// </summary>
    protected PluginCommandBase(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public abstract string CommandId { get; }

    /// <inheritdoc />
    public abstract string DisplayName { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <inheritdoc />
    public virtual string? DetailedInstructions => null;

    /// <inheritdoc />
    public virtual string? IconResource => null;

    /// <inheritdoc />
    public virtual string? KeyboardShortcut => null;

    /// <inheritdoc />
    public virtual CommandCategory Category => CommandCategory.Plugin;

    /// <inheritdoc />
    public virtual int Priority => 1000;

    /// <inheritdoc />
    public virtual bool IsEnabled => true;

    /// <inheritdoc />
    public virtual bool IsVisible => true;

    /// <inheritdoc />
    public virtual IReadOnlyDictionary<string, CommandParameter>? Parameters => null;

    /// <inheritdoc />
    public event EventHandler? StateChanged;

    /// <summary>
    /// Raise the StateChanged event
    /// </summary>
    protected virtual void OnStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public virtual Task PrepareAsync(ICommandContext context, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (Logger != null)
            LogPreparingCommand(Logger, CommandId, null);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual bool CanExecute(ICommandContext context, IDictionary<string, object>? parameters = null)
    {
        ThrowIfDisposed();

        // Validate required parameters
        if (Parameters != null && parameters != null)
        {
            foreach (var param in Parameters.Values)
            {
                if (param.IsRequired && !parameters.ContainsKey(param.Name))
                {
                    if (Logger != null)
                        LogMissingParameter(Logger, param.Name, CommandId, null);
                    return false;
                }
            }
        }

        return OnCanExecute(context, parameters);
    }

    /// <summary>
    /// Override this method to implement custom CanExecute logic
    /// </summary>
    protected virtual bool OnCanExecute(ICommandContext context, IDictionary<string, object>? parameters)
    {
        return IsEnabled;
    }

    /// <inheritdoc />
    public async Task<CommandResult> ExecuteAsync(ICommandContext context, IDictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            // Validate parameters
            var validationResult = ValidateParameters(parameters);
            if (!validationResult.IsSuccess)
            {
                return validationResult;
            }

            // Normalize parameters with defaults
            var normalizedParameters = NormalizeParameters(parameters);

            if (Logger != null)
                LogExecutingCommand(Logger, CommandId, null);

            // Execute the command
            var result = await OnExecuteAsync(context, normalizedParameters, cancellationToken).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                if (Logger != null)
                    LogCommandSuccess(Logger, CommandId, null);
            }
            else
            {
                if (Logger != null)
                    LogCommandFailure(Logger, CommandId, result.Message, null);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            if (Logger != null)
                LogCommandCancelled(Logger, CommandId, null);
            return CommandResult.Failure("Command was cancelled");
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
        {
            if (Logger != null)
                LogCommandError(Logger, CommandId, ex);
            return CommandResult.Failure($"Unexpected error: {ex.Message}", ex);
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    /// <summary>
    /// Override this method to implement the actual command execution
    /// </summary>
    protected abstract Task<CommandResult> OnExecuteAsync(ICommandContext context, IDictionary<string, object> parameters, CancellationToken cancellationToken);

    /// <inheritdoc />
    public virtual Task CleanupAsync(ICommandContext context, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (Logger != null)
            LogCleaningUpCommand(Logger, CommandId, null);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Validate command parameters
    /// </summary>
    protected virtual CommandResult ValidateParameters(IDictionary<string, object>? parameters)
    {
        if (Parameters == null)
        {
            return CommandResult.Success();
        }

        parameters ??= new Dictionary<string, object>();

        foreach (var param in Parameters.Values)
        {
            if (param.IsRequired && !parameters.ContainsKey(param.Name))
            {
                return CommandResult.Failure($"Required parameter '{param.Name}' is missing");
            }

            if (parameters.TryGetValue(param.Name, out var value) && value != null)
            {
                // Type validation
                if (!param.Type.IsAssignableFrom(value.GetType()))
                {
                    return CommandResult.Failure($"Parameter '{param.Name}' must be of type {param.Type.Name}");
                }

                // Valid values validation
                if (param.ValidValues != null && !param.ValidValues.Contains(value))
                {
                    return CommandResult.Failure($"Parameter '{param.Name}' has invalid value. Valid values: {string.Join(", ", param.ValidValues)}");
                }
            }
        }

        return CommandResult.Success();
    }

    /// <summary>
    /// Normalize parameters by applying default values
    /// </summary>
    protected virtual IDictionary<string, object> NormalizeParameters(IDictionary<string, object>? parameters)
    {
        var result = new Dictionary<string, object>(parameters ?? new Dictionary<string, object>());

        if (Parameters != null)
        {
            foreach (var param in Parameters.Values)
            {
                if (!result.ContainsKey(param.Name) && param.DefaultValue != null)
                {
                    result[param.Name] = param.DefaultValue;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Get a typed parameter value
    /// </summary>
    protected static T? GetParameter<T>(IDictionary<string, object> parameters, string name, T? defaultValue = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        if (parameters.TryGetValue(name, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    /// <summary>
    /// Report progress for long-running operations
    /// </summary>
    protected static void ReportProgress(ICommandContext context, int percentage, string? description = null, bool isCancellable = true)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Progress?.Report(new CommandProgress
        {
            Percentage = percentage,
            Description = description,
            IsCancellable = isCancellable
        });
    }

    /// <summary>
    /// Check if the operation was cancelled
    /// </summary>
    protected static void ThrowIfCancelled(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// Throw if the command has been disposed
    /// </summary>
    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, GetType());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose pattern implementation
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                OnDispose();
            }
            _disposed = true;
        }
    }

    /// <summary>
    /// Override this method to implement custom disposal logic
    /// </summary>
    protected virtual void OnDispose()
    {
        // Override in derived classes
    }
}
