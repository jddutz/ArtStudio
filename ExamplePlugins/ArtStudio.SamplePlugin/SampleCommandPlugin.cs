using System;
using System.Collections.Generic;
using ArtStudio.Core;
using ArtStudio.ExamplePlugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArtStudio.SamplePlugin;

/// <summary>
/// Sample plugin that demonstrates command implementation
/// </summary>
[PluginMetadata("sample-command-plugin", "Sample Command Plugin", "Demonstrates plugin command implementation", "ArtStudio Team", "1.0.0")]
public class SampleCommandPlugin : ICommandPlugin
{
    private readonly List<IPluginCommand> _commands = new();
    private ILogger<SampleCommandPlugin>? _logger;

    /// <inheritdoc />
    public string Id => "sample-command-plugin";

    /// <inheritdoc />
    public string Name => "Sample Command Plugin";

    /// <inheritdoc />
    public string Description => "Demonstrates plugin command implementation";

    /// <inheritdoc />
    public Version Version => new(1, 0, 0);

    /// <inheritdoc />
    public string Author => "ArtStudio Team";

    /// <inheritdoc />
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc />
    public IEnumerable<IPluginCommand> Commands => _commands;

    /// <inheritdoc />
    public void Initialize(IPluginContext context)
    {
        _logger = context.ServiceProvider.GetService<ILogger<SampleCommandPlugin>>();
        _logger?.LogInformation("Initializing Sample Command Plugin");

        // Create commands
        var loggerFactory = context.ServiceProvider.GetService<ILoggerFactory>();

        _commands.Add(new NewDocumentCommand(loggerFactory?.CreateLogger<NewDocumentCommand>()));
        _commands.Add(new SaveDocumentCommand(loggerFactory?.CreateLogger<SaveDocumentCommand>()));
        _commands.Add(new SampleFilterCommand(loggerFactory?.CreateLogger<SampleFilterCommand>()));
    }

    /// <inheritdoc />
    public void RegisterCommands(ICommandRegistry commandRegistry)
    {
        _logger?.LogInformation("Registering commands for Sample Command Plugin");

        foreach (var command in _commands)
        {
            try
            {
                commandRegistry.RegisterCommand(command);
                _logger?.LogDebug("Registered command: {CommandId}", command.CommandId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to register command: {CommandId}", command.CommandId);
            }
        }
    }

    /// <inheritdoc />
    public void UnregisterCommands(ICommandRegistry commandRegistry)
    {
        _logger?.LogInformation("Unregistering commands for Sample Command Plugin");

        foreach (var command in _commands)
        {
            try
            {
                commandRegistry.UnregisterCommand(command.CommandId);
                _logger?.LogDebug("Unregistered command: {CommandId}", command.CommandId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to unregister command: {CommandId}", command.CommandId);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _logger?.LogInformation("Disposing Sample Command Plugin");

        foreach (var command in _commands)
        {
            try
            {
                command.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error disposing command: {CommandId}", command.CommandId);
            }
        }

        _commands.Clear();
    }
}
