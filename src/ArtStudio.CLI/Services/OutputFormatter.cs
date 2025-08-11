using System.Globalization;
using System.Text;
using System.Text.Json;
using ArtStudio.CLI.Models;
using ArtStudio.Core;

namespace ArtStudio.CLI.Services;

/// <summary>
/// Service for formatting output in different formats
/// </summary>
public class OutputFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Output format options
    /// </summary>
    public enum OutputFormat
    {
        Text,
        Json,
        Yaml,
        Table
    }

    /// <summary>
    /// Format command result
    /// </summary>
#pragma warning disable CA1822 // Mark members as static
    // Keeping as instance method to support dependency injection and unit testing
    public string FormatCommandResult(CommandResult result, OutputFormat format = OutputFormat.Text)
#pragma warning restore CA1822 // Mark members as static
    {
        ArgumentNullException.ThrowIfNull(result);

        return format switch
        {
            OutputFormat.Json => FormatCommandResultAsJson(result),
            OutputFormat.Table => FormatCommandResultAsTable(result),
            OutputFormat.Yaml => FormatCommandResultAsYaml(result),
            _ => FormatCommandResultAsText(result)
        };
    }

    /// <summary>
    /// Format batch result
    /// </summary>
#pragma warning disable CA1822 // Mark members as static
    // Keeping as instance method to support dependency injection and unit testing
    public string FormatBatchResult(BatchResult result, OutputFormat format = OutputFormat.Text)
#pragma warning restore CA1822 // Mark members as static
    {
        ArgumentNullException.ThrowIfNull(result);

        return format switch
        {
            OutputFormat.Json => FormatBatchResultAsJson(result),
            OutputFormat.Table => FormatBatchResultAsTable(result),
            OutputFormat.Yaml => FormatBatchResultAsYaml(result),
            _ => FormatBatchResultAsText(result)
        };
    }

    /// <summary>
    /// Format command list
    /// </summary>
#pragma warning disable CA1822 // Mark members as static
    // Keeping as instance method to support dependency injection and unit testing
    public string FormatCommandList(IEnumerable<IPluginCommand> commands, OutputFormat format = OutputFormat.Text)
#pragma warning restore CA1822 // Mark members as static
    {
        return format switch
        {
            OutputFormat.Json => FormatCommandListAsJson(commands),
            OutputFormat.Table => FormatCommandListAsTable(commands),
            OutputFormat.Yaml => FormatCommandListAsYaml(commands),
            _ => FormatCommandListAsText(commands)
        };
    }

    #region Text Formatting

    private static string FormatCommandResultAsText(CommandResult result)
    {
        var output = new StringBuilder();

        if (result.IsSuccess)
        {
            output.AppendLine("✓ Command executed successfully");
            if (!string.IsNullOrWhiteSpace(result.Message))
                output.AppendLine(CultureInfo.InvariantCulture, $"  {result.Message}");
        }
        else
        {
            output.AppendLine("✗ Command failed");
            if (!string.IsNullOrWhiteSpace(result.Message))
                output.AppendLine(CultureInfo.InvariantCulture, $"  Error: {result.Message}");
        }

        if (result.Data?.Count > 0)
        {
            output.AppendLine("  Output data:");
            foreach (var (key, value) in result.Data)
            {
                output.AppendLine(CultureInfo.InvariantCulture, $"    {key}: {value}");
            }
        }

        return output.ToString();
    }

    private static string FormatBatchResultAsText(BatchResult result)
    {
        var output = new StringBuilder();

        output.AppendLine(CultureInfo.InvariantCulture, $"Batch execution completed:");
        output.AppendLine(CultureInfo.InvariantCulture, $"  ✓ Successful: {result.SuccessCount}");
        output.AppendLine(CultureInfo.InvariantCulture, $"  ✗ Failed: {result.FailureCount}");
        output.AppendLine(CultureInfo.InvariantCulture, $"  Duration: {result.CompletedAt - result.StartedAt:mm\\:ss\\.fff}");
        output.AppendLine();

        if (result.Results.Any())
        {
            output.AppendLine("Command results:");
            foreach (var commandResult in result.Results)
            {
                var status = commandResult.Result.IsSuccess ? "✓" : "✗";
                output.AppendLine(CultureInfo.InvariantCulture, $"  {status} {commandResult.CommandId}");
                if (!commandResult.Result.IsSuccess && !string.IsNullOrWhiteSpace(commandResult.Result.Message))
                {
                    output.AppendLine(CultureInfo.InvariantCulture, $"    Error: {commandResult.Result.Message}");
                }
            }
        }

        return output.ToString();
    }

    private static string FormatCommandListAsText(IEnumerable<IPluginCommand> commands)
    {
        var output = new StringBuilder();
        var commandList = commands.ToList();

        if (commandList.Count == 0)
        {
            return "No commands available";
        }

        foreach (var category in Enum.GetValues<CommandCategory>())
        {
            var categoryCommands = commandList.Where(c => c.Category == category).ToList();
            if (categoryCommands.Count == 0)
                continue;

            output.AppendLine(CultureInfo.InvariantCulture, $"{category}:");
            foreach (var command in categoryCommands.OrderBy(c => c.Priority).ThenBy(c => c.DisplayName))
            {
                output.Append(CultureInfo.InvariantCulture, $"  {command.CommandId}");
                output.Append(CultureInfo.InvariantCulture, $" - {command.Description}");

                if (!string.IsNullOrWhiteSpace(command.KeyboardShortcut))
                    output.Append(CultureInfo.InvariantCulture, $" ({command.KeyboardShortcut})");

                output.AppendLine();
            }
            output.AppendLine();
        }

        return output.ToString();
    }

    #endregion

    #region JSON Formatting

    private static string FormatCommandResultAsJson(CommandResult result)
    {
        var data = new
        {
            success = result.IsSuccess,
            message = result.Message,
            data = result.Data,
            exception = result.Exception?.Message
        };

        return JsonSerializer.Serialize(data, JsonOptions);
    }

    private static string FormatBatchResultAsJson(BatchResult result)
    {
        var data = new
        {
            success = result.IsSuccess,
            successCount = result.SuccessCount,
            failureCount = result.FailureCount,
            startedAt = result.StartedAt,
            completedAt = result.CompletedAt,
            duration = result.CompletedAt - result.StartedAt,
            results = result.Results.Select(r => new
            {
                commandId = r.CommandId,
                success = r.Result.IsSuccess,
                message = r.Result.Message,
                executedAt = r.ExecutedAt,
                data = r.Result.Data,
                exception = r.Exception?.Message
            })
        };

        return JsonSerializer.Serialize(data, JsonOptions);
    }

    private static string FormatCommandListAsJson(IEnumerable<IPluginCommand> commands)
    {
        var data = commands.Select(c => new
        {
            commandId = c.CommandId,
            displayName = c.DisplayName,
            description = c.Description,
            category = c.Category.ToString(),
            priority = c.Priority,
            keyboardShortcut = c.KeyboardShortcut,
            isEnabled = c.IsEnabled,
            isVisible = c.IsVisible,
            parameters = c.Parameters?.Values.Select(p => new
            {
                name = p.Name,
                type = p.Type?.Name,
                isRequired = p.IsRequired,
                defaultValue = p.DefaultValue,
                description = p.Description,
                validValues = p.ValidValues
            })
        });

        return JsonSerializer.Serialize(data, JsonOptions);
    }

    #endregion

    #region Table Formatting

    private static string FormatCommandResultAsTable(CommandResult result)
    {
        var output = new StringBuilder();

        output.AppendLine("┌─────────┬────────────────────────────────────────┐");
        output.AppendLine("│ Field   │ Value                                  │");
        output.AppendLine("├─────────┼────────────────────────────────────────┤");
        output.AppendLine(CultureInfo.InvariantCulture, $"│ Status  │ {(result.IsSuccess ? "Success" : "Failed"),-38} │");

        if (!string.IsNullOrWhiteSpace(result.Message))
        {
            var message = result.Message.Length > 38 ? result.Message[..35] + "..." : result.Message;
            output.AppendLine(CultureInfo.InvariantCulture, $"│ Message │ {message,-38} │");
        }

        output.AppendLine("└─────────┴────────────────────────────────────────┘");

        return output.ToString();
    }

    private static string FormatBatchResultAsTable(BatchResult result)
    {
        var output = new StringBuilder();

        // Summary table
        output.AppendLine("┌──────────┬───────┐");
        output.AppendLine("│ Status   │ Count │");
        output.AppendLine("├──────────┼───────┤");
        output.AppendLine(CultureInfo.InvariantCulture, $"│ Success  │ {result.SuccessCount,5} │");
        output.AppendLine(CultureInfo.InvariantCulture, $"│ Failed   │ {result.FailureCount,5} │");
        output.AppendLine("└──────────┴───────┘");

        return output.ToString();
    }

    private static string FormatCommandListAsTable(IEnumerable<IPluginCommand> commands)
    {
        var commandList = commands.ToList();
        if (commandList.Count == 0)
            return "No commands available";

        var output = new StringBuilder();

        output.AppendLine("┌─────────────────────┬────────────────────────────────┬──────────┐");
        output.AppendLine("│ Command ID          │ Description                    │ Category │");
        output.AppendLine("├─────────────────────┼────────────────────────────────┼──────────┤");

        foreach (var command in commandList.OrderBy(c => c.Category).ThenBy(c => c.CommandId))
        {
            var commandId = command.CommandId.Length > 19 ? command.CommandId[..16] + "..." : command.CommandId;
            var description = command.Description.Length > 30 ? command.Description[..27] + "..." : command.Description;
            var category = command.Category.ToString();

            output.AppendLine(CultureInfo.InvariantCulture, $"│ {commandId,-19} │ {description,-30} │ {category,-8} │");
        }

        output.AppendLine("└─────────────────────┴────────────────────────────────┴──────────┘");

        return output.ToString();
    }

    #endregion

    #region YAML Formatting

    private static string FormatCommandResultAsYaml(CommandResult result)
    {
        var output = new StringBuilder();

        output.AppendLine("result:");
        output.AppendLine(CultureInfo.InvariantCulture, $"  success: {result.IsSuccess.ToString().ToUpperInvariant()}");

        if (!string.IsNullOrWhiteSpace(result.Message))
            output.AppendLine(CultureInfo.InvariantCulture, $"  message: \"{result.Message}\"");

        if (result.Data?.Count > 0)
        {
            output.AppendLine("  data:");
            foreach (var (key, value) in result.Data)
            {
                output.AppendLine(CultureInfo.InvariantCulture, $"    {key}: \"{value}\"");
            }
        }

        return output.ToString();
    }

    private static string FormatBatchResultAsYaml(BatchResult result)
    {
        var output = new StringBuilder();

        output.AppendLine("batch:");
        output.AppendLine(CultureInfo.InvariantCulture, $"  success: {result.IsSuccess.ToString().ToUpperInvariant()}");
        output.AppendLine(CultureInfo.InvariantCulture, $"  successCount: {result.SuccessCount}");
        output.AppendLine(CultureInfo.InvariantCulture, $"  failureCount: {result.FailureCount}");
        output.AppendLine(CultureInfo.InvariantCulture, $"  startedAt: \"{result.StartedAt:yyyy-MM-ddTHH:mm:ss.fffZ}\"");
        output.AppendLine(CultureInfo.InvariantCulture, $"  completedAt: \"{result.CompletedAt:yyyy-MM-ddTHH:mm:ss.fffZ}\"");

        if (result.Results.Any())
        {
            output.AppendLine("  results:");
            foreach (var commandResult in result.Results)
            {
                output.AppendLine(CultureInfo.InvariantCulture, $"    - commandId: \"{commandResult.CommandId}\"");
                output.AppendLine(CultureInfo.InvariantCulture, $"      success: {commandResult.Result.IsSuccess.ToString().ToUpperInvariant()}");
                if (!string.IsNullOrWhiteSpace(commandResult.Result.Message))
                    output.AppendLine(CultureInfo.InvariantCulture, $"      message: \"{commandResult.Result.Message}\"");
            }
        }

        return output.ToString();
    }

    private static string FormatCommandListAsYaml(IEnumerable<IPluginCommand> commands)
    {
        var output = new StringBuilder();

        output.AppendLine("commands:");
        foreach (var command in commands.OrderBy(c => c.Category).ThenBy(c => c.CommandId))
        {
            output.AppendLine(CultureInfo.InvariantCulture, $"  - commandId: \"{command.CommandId}\"");
            output.AppendLine(CultureInfo.InvariantCulture, $"    displayName: \"{command.DisplayName}\"");
            output.AppendLine(CultureInfo.InvariantCulture, $"    description: \"{command.Description}\"");
            output.AppendLine(CultureInfo.InvariantCulture, $"    category: \"{command.Category}\"");
            output.AppendLine(CultureInfo.InvariantCulture, $"    priority: {command.Priority}");
            output.AppendLine(CultureInfo.InvariantCulture, $"    enabled: {command.IsEnabled.ToString().ToUpperInvariant()}");
            output.AppendLine(CultureInfo.InvariantCulture, $"    visible: {command.IsVisible.ToString().ToUpperInvariant()}");

            if (!string.IsNullOrWhiteSpace(command.KeyboardShortcut))
                output.AppendLine(CultureInfo.InvariantCulture, $"    keyboardShortcut: \"{command.KeyboardShortcut}\"");
        }

        return output.ToString();
    }

    #endregion
}
