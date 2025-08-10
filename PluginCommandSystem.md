# Plugin Command System

The ArtStudio Plugin Command System provides a robust, extensible architecture for implementing commands that work seamlessly in both WPF/MVVM and CLI contexts. This system enables plugins to define reusable commands with proper error handling, logging, progress reporting, and parameter validation.

## Key Components

### Core Interfaces

- **`IPluginCommand`**: Base interface for all plugin commands
- **`ICommandContext`**: Execution context providing services and configuration
- **`ICommandRegistry`**: Registry for managing and discovering commands
- **`ICommandPlugin`**: Interface for plugins that provide commands

### Core Services

- **`PluginCommandBase`**: Base implementation class for commands
- **`CommandRegistry`**: Default implementation of command registry
- **`CommandContext`**: Default implementation of command context
- **`CommandPluginManager`**: Manages command plugins and registry integration

### WPF Integration

- **`PluginCommandWrapper`**: Wraps plugin commands as WPF ICommand
- **`CommandsViewModel`**: View model for WPF command integration

### CLI Support

- **`CliCommandExecutor`**: Executes commands in CLI/automation scenarios

## Creating a Plugin Command

### 1. Basic Command Implementation

```csharp
public class MyCommand : PluginCommandBase
{
    public override string CommandId => "my-command";
    public override string DisplayName => "My Command";
    public override string Description => "Does something useful";
    public override CommandCategory Category => CommandCategory.Edit;

    protected override async Task<CommandResult> OnExecuteAsync(
        ICommandContext context,
        IDictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        // Your command logic here
        return CommandResult.Success("Command completed successfully");
    }
}
```

### 2. Command with Parameters

```csharp
public class ParameterizedCommand : PluginCommandBase
{
    public override IReadOnlyDictionary<string, CommandParameter> Parameters { get; } =
        new Dictionary<string, CommandParameter>
        {
            ["width"] = new CommandParameter
            {
                Name = "width",
                Type = typeof(int),
                IsRequired = true,
                Description = "Width in pixels"
            },
            ["format"] = new CommandParameter
            {
                Name = "format",
                Type = typeof(string),
                IsRequired = false,
                DefaultValue = "PNG",
                ValidValues = new[] { "PNG", "JPEG", "BMP" }
            }
        };

    protected override async Task<CommandResult> OnExecuteAsync(
        ICommandContext context,
        IDictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        var width = GetParameter<int>(parameters, "width");
        var format = GetParameter<string>(parameters, "format", "PNG");

        // Use parameters in your logic
        return CommandResult.Success($"Processed with width: {width}, format: {format}");
    }
}
```

### 3. Command with Progress Reporting

```csharp
protected override async Task<CommandResult> OnExecuteAsync(
    ICommandContext context,
    IDictionary<string, object> parameters,
    CancellationToken cancellationToken)
{
    ReportProgress(context, 0, "Starting operation...");

    for (int i = 0; i < 100; i++)
    {
        ThrowIfCancelled(cancellationToken);

        // Do work
        await Task.Delay(10, cancellationToken);

        ReportProgress(context, i + 1, $"Processing step {i + 1}/100");
    }

    return CommandResult.Success("Operation completed");
}
```

## Creating a Command Plugin

```csharp
[PluginMetadata("my-plugin", "My Plugin", "Provides useful commands", "Author", "1.0.0")]
public class MyCommandPlugin : ICommandPlugin
{
    private readonly List<IPluginCommand> _commands = new();

    public IEnumerable<IPluginCommand> Commands => _commands;

    public void Initialize(IPluginContext context)
    {
        var logger = context.ServiceProvider.GetService<ILogger<MyCommand>>();
        _commands.Add(new MyCommand(logger));
    }

    public void RegisterCommands(ICommandRegistry commandRegistry)
    {
        foreach (var command in _commands)
        {
            commandRegistry.RegisterCommand(command);
        }
    }

    public void UnregisterCommands(ICommandRegistry commandRegistry)
    {
        foreach (var command in _commands)
        {
            commandRegistry.UnregisterCommand(command.CommandId);
        }
    }

    // ... other IPlugin members
}
```

## WPF Integration

### 1. Menu Binding

```xml
<Menu>
    <MenuItem Header="File">
        <MenuItem.ItemsSource>
            <Binding Path="FileCommands" />
        </MenuItem.ItemsSource>
        <MenuItem.ItemContainerStyle>
            <Style TargetType="MenuItem">
                <Setter Property="Header" Value="{Binding PluginCommand.DisplayName}" />
                <Setter Property="Command" Value="{Binding}" />
                <Setter Property="ToolTip" Value="{Binding PluginCommand.Description}" />
            </Style>
        </MenuItem.ItemContainerStyle>
    </MenuItem>
</Menu>
```

### 2. Keyboard Shortcuts

```csharp
// In your main window or view model
public void SetupKeyboardShortcuts()
{
    foreach (var (shortcut, command) in CommandsViewModel.ShortcutCommands)
    {
        var keyBinding = new KeyBinding(command, ParseKeyGesture(shortcut));
        InputBindings.Add(keyBinding);
    }
}
```

### 3. Progress Handling

```csharp
public MainViewModel()
{
    CommandsViewModel.CommandProgressReported += OnCommandProgress;
    CommandsViewModel.CommandExecutionCompleted += OnCommandCompleted;
}

private void OnCommandProgress(object sender, CommandProgressEventArgs e)
{
    ProgressText = e.Progress.Description;
    ProgressValue = e.Progress.Percentage;
}
```

## CLI Usage

### 1. Command Line Execution

```bash
# Execute a command with parameters
artstudio new-document --width 1920 --height 1080 --backgroundColor White

# Execute a filter command
artstudio apply-blur-filter --radius 5.0 --previewMode false

# Get help for a command
artstudio --help new-document

# List all available commands
artstudio --list-commands
```

### 2. Programmatic CLI Execution

```csharp
var executor = serviceProvider.GetRequiredService<CliCommandExecutor>();

// Execute single command
var result = await executor.ExecuteCommandAsync("new-document", new Dictionary<string, object>
{
    ["width"] = 800,
    ["height"] = 600
});

// Execute batch commands
var batch = new[]
{
    new BatchCommandRequest { CommandId = "new-document", Parameters = new Dictionary<string, object> { ["width"] = 800 } },
    new BatchCommandRequest { CommandId = "apply-blur-filter", Parameters = new Dictionary<string, object> { ["radius"] = 3.0 } }
};

var batchResult = await executor.ExecuteBatchAsync(batch);
```

## Dependency Injection Setup

```csharp
// In your service configuration
services.AddSingleton<ICommandRegistry, CommandRegistry>();
services.AddSingleton<CommandPluginManager>();
services.AddTransient<CliCommandExecutor>();
services.AddTransient<CommandsViewModel>();

// Setup command plugins after plugin manager is initialized
var commandPluginManager = serviceProvider.GetRequiredService<CommandPluginManager>();
commandPluginManager.RegisterAllCommandPlugins();
```

## Best Practices

### Command Design

1. **Single Responsibility**: Each command should do one thing well
2. **Idempotent**: Commands should be safe to run multiple times
3. **Cancellable**: Support cancellation for long-running operations
4. **Error Handling**: Provide meaningful error messages
5. **Progress Reporting**: Report progress for operations > 1 second

### Parameter Design

1. **Required vs Optional**: Minimize required parameters
2. **Default Values**: Provide sensible defaults
3. **Validation**: Validate parameters early
4. **Type Safety**: Use appropriate types for parameters

### Performance

1. **Async Operations**: Use async/await for I/O operations
2. **Cancellation**: Check cancellation tokens regularly
3. **Resource Management**: Dispose resources properly
4. **Memory Usage**: Be mindful of memory usage in long operations

### Testing

1. **Unit Tests**: Test command logic in isolation
2. **Integration Tests**: Test command registration and execution
3. **Parameter Tests**: Test parameter validation
4. **Error Tests**: Test error handling scenarios

## Error Handling

The command system provides multiple layers of error handling:

1. **Parameter Validation**: Automatic validation of required parameters and types
2. **Command-Level**: Custom validation in `OnCanExecute` and `OnExecuteAsync`
3. **Wrapper-Level**: Automatic exception catching and logging
4. **Registry-Level**: Safe command registration/unregistration

Commands should return `CommandResult.Failure()` for expected errors and throw exceptions only for unexpected errors.

## Logging

The system integrates with Microsoft.Extensions.Logging:

```csharp
protected override async Task<CommandResult> OnExecuteAsync(...)
{
    Logger?.LogInformation("Starting command execution");

    try
    {
        // Command logic
        Logger?.LogDebug("Intermediate step completed");
        return CommandResult.Success();
    }
    catch (Exception ex)
    {
        Logger?.LogError(ex, "Command failed");
        return CommandResult.Failure("Command failed", ex);
    }
}
```

This architecture provides a flexible, testable, and maintainable foundation for implementing commands in both interactive and automated scenarios.
