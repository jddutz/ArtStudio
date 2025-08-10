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
    protected readonly ILogger? Logger;
    private bool _disposed;

    /// <summary>
    /// Initialize the command with optional logger
    /// </summary>
    protected PluginCommandBase(ILogger? logger = null)
    {
        Logger = logger;
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
        Logger?.LogDebug("Preparing command {CommandId}", CommandId);
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
                    Logger?.LogWarning("Required parameter {ParameterName} is missing for command {CommandId}",
                        param.Name, CommandId);
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

            Logger?.LogInformation("Executing command {CommandId}", CommandId);

            // Execute the command
            var result = await OnExecuteAsync(context, normalizedParameters, cancellationToken);

            if (result.IsSuccess)
            {
                Logger?.LogInformation("Command {CommandId} executed successfully", CommandId);
            }
            else
            {
                Logger?.LogWarning("Command {CommandId} failed: {Message}", CommandId, result.Message);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            Logger?.LogInformation("Command {CommandId} was cancelled", CommandId);
            return CommandResult.Failure("Command was cancelled");
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Unexpected error executing command {CommandId}", CommandId);
            return CommandResult.Failure($"Unexpected error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Override this method to implement the actual command execution
    /// </summary>
    protected abstract Task<CommandResult> OnExecuteAsync(ICommandContext context, IDictionary<string, object> parameters, CancellationToken cancellationToken);

    /// <inheritdoc />
    public virtual Task CleanupAsync(ICommandContext context, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        Logger?.LogDebug("Cleaning up command {CommandId}", CommandId);
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
    protected T? GetParameter<T>(IDictionary<string, object> parameters, string name, T? defaultValue = default)
    {
        if (parameters.TryGetValue(name, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    /// <summary>
    /// Report progress for long-running operations
    /// </summary>
    protected void ReportProgress(ICommandContext context, int percentage, string? description = null, bool isCancellable = true)
    {
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
    protected void ThrowIfCancelled(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// Throw if the command has been disposed
    /// </summary>
    protected void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
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
