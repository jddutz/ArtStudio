using System;
using System.Collections.Generic;
using ArtStudio.Core;

namespace ArtStudio.Core.Services;

/// <summary>
/// Default implementation of ICommandContext
/// </summary>
public class CommandContext : ICommandContext
{
    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; }


    /// <inheritdoc />
    public IConfigurationManager ConfigurationManager { get; }

    /// <inheritdoc />
    public CommandExecutionMode ExecutionMode { get; }

    /// <inheritdoc />
    public IProgress<CommandProgress>? Progress { get; }

    /// <inheritdoc />
    public IDictionary<string, object> Data { get; }

    /// <summary>
    /// Initialize the command context
    /// </summary>
    public CommandContext(
        IServiceProvider serviceProvider,
        IConfigurationManager configurationManager,
        CommandExecutionMode executionMode = CommandExecutionMode.Interactive,
        IProgress<CommandProgress>? progress = null,
        IDictionary<string, object>? data = null)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        ConfigurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
        ExecutionMode = executionMode;
        Progress = progress;
        Data = data ?? new Dictionary<string, object>();
    }
}
