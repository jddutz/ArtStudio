using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ArtStudio.Core;

namespace ArtStudio.CLI.Services;

/// <summary>
/// Service for providing help information about commands
/// </summary>
public class HelpProvider
{
    private readonly ICommandRegistry _commandRegistry;

    /// <summary>
    /// Initialize the help provider
    /// </summary>
    public HelpProvider(ICommandRegistry commandRegistry)
    {
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
    }

    /// <summary>
    /// Get help information for a specific command
    /// </summary>
    public string GetCommandHelp(string commandId)
    {
        var command = _commandRegistry.GetCommand(commandId);
        if (command == null)
            return $"Command '{commandId}' not found";

        var help = new StringBuilder();
        help.AppendLine(CultureInfo.InvariantCulture, $"Command: {command.CommandId}");
        help.AppendLine(CultureInfo.InvariantCulture, $"Name: {command.DisplayName}");
        help.AppendLine(CultureInfo.InvariantCulture, $"Description: {command.Description}");

        if (!string.IsNullOrWhiteSpace(command.DetailedInstructions))
        {
            help.AppendLine();
            help.AppendLine("Instructions:");
            help.AppendLine(command.DetailedInstructions);
        }

        if (!string.IsNullOrWhiteSpace(command.KeyboardShortcut))
        {
            help.AppendLine(CultureInfo.InvariantCulture, $"Keyboard Shortcut: {command.KeyboardShortcut}");
        }

        help.AppendLine(CultureInfo.InvariantCulture, $"Category: {command.Category}");
        help.AppendLine(CultureInfo.InvariantCulture, $"Priority: {command.Priority}");

        if (command.Parameters?.Count > 0)
        {
            help.AppendLine();
            help.AppendLine("Parameters:");
            foreach (var param in command.Parameters.Values.OrderBy(p => p.IsRequired ? 0 : 1).ThenBy(p => p.Name))
            {
                help.Append(CultureInfo.InvariantCulture, $"  --{param.Name}");

                if (param.Type != null)
                    help.Append(CultureInfo.InvariantCulture, $" ({param.Type.Name})");

                if (param.IsRequired)
                    help.Append(" [required]");

                if (param.DefaultValue != null)
                    help.Append(CultureInfo.InvariantCulture, $" [default: {param.DefaultValue}]");

                help.AppendLine();

                if (!string.IsNullOrWhiteSpace(param.Description))
                {
                    help.AppendLine(CultureInfo.InvariantCulture, $"    {param.Description}");
                }

                if (param.ValidValues?.Count > 0)
                {
                    help.AppendLine(CultureInfo.InvariantCulture, $"    Valid values: {string.Join(", ", param.ValidValues)}");
                }
            }
        }

        help.AppendLine();
        help.AppendLine("Usage:");
        help.Append(CultureInfo.InvariantCulture, $"  artstudio execute {command.CommandId}");

        if (command.Parameters?.Count > 0)
        {
            var requiredParams = command.Parameters.Values.Where(p => p.IsRequired);
            foreach (var param in requiredParams)
            {
                help.Append(CultureInfo.InvariantCulture, $" --{param.Name} <value>");
            }

            var optionalParams = command.Parameters.Values.Where(p => !p.IsRequired);
            if (optionalParams.Any())
            {
                help.Append(" [options]");
            }
        }

        help.AppendLine();

        return help.ToString();
    }

    /// <summary>
    /// List all available commands
    /// </summary>
    public string ListCommands(CommandCategory? filterCategory = null)
    {
        var commands = _commandRegistry.Commands.ToList();
        if (commands.Count == 0)
            return "No commands available";

        if (filterCategory.HasValue)
        {
            commands = commands.Where(c => c.Category == filterCategory.Value).ToList();
            if (commands.Count == 0)
                return $"No commands available in category '{filterCategory.Value}'";
        }

        var help = new StringBuilder();
        help.AppendLine("Available commands:");
        help.AppendLine();

        var categories = filterCategory.HasValue
            ? new[] { filterCategory.Value }
            : Enum.GetValues<CommandCategory>();

        foreach (var category in categories)
        {
            var categoryCommands = commands.Where(c => c.Category == category).ToList();
            if (categoryCommands.Count == 0)
                continue;

            help.AppendLine(CultureInfo.InvariantCulture, $"{category}:");
            foreach (var command in categoryCommands.OrderBy(c => c.Priority).ThenBy(c => c.DisplayName))
            {
                help.Append(CultureInfo.InvariantCulture, $"  {command.CommandId}");
                help.Append(CultureInfo.InvariantCulture, $" - {command.Description}");

                if (!string.IsNullOrWhiteSpace(command.KeyboardShortcut))
                    help.Append(CultureInfo.InvariantCulture, $" ({command.KeyboardShortcut})");

                help.AppendLine();
            }
            help.AppendLine();
        }

        help.AppendLine("Use 'artstudio help <command-id>' for detailed help on a specific command.");
        return help.ToString();
    }

    /// <summary>
    /// Get general application help
    /// </summary>
#pragma warning disable CA1822 // Mark members as static
    // Keeping as instance method to support dependency injection and unit testing
    public string GetApplicationHelp()
#pragma warning restore CA1822 // Mark members as static
    {
        var help = new StringBuilder();
        help.AppendLine("ArtStudio CLI - Command-line interface for ArtStudio automation");
        help.AppendLine();
        help.AppendLine("Usage:");
        help.AppendLine("  artstudio <command> [options]");
        help.AppendLine();
        help.AppendLine("Commands:");
        help.AppendLine("  execute <command-id>  Execute a specific command");
        help.AppendLine("  batch <file>          Execute commands from batch file");
        help.AppendLine("  list [category]       List available commands");
        help.AppendLine("  help [command-id]     Show help information");
        help.AppendLine();
        help.AppendLine("Global Options:");
        help.AppendLine("  --verbose, -v         Enable verbose logging");
        help.AppendLine("  --quiet, -q           Suppress output except errors");
        help.AppendLine("  --config <file>       Specify configuration file");
        help.AppendLine("  --help, -h            Show help information");
        help.AppendLine("  --version             Show version information");
        help.AppendLine();
        help.AppendLine("Examples:");
        help.AppendLine("  artstudio list");
        help.AppendLine("  artstudio help save-image");
        help.AppendLine("  artstudio execute save-image --path output.png --format PNG");
        help.AppendLine("  artstudio batch commands.json");
        help.AppendLine();

        return help.ToString();
    }

    /// <summary>
    /// Get command usage syntax
    /// </summary>
    public string GetCommandUsage(string commandId)
    {
        var command = _commandRegistry.GetCommand(commandId);
        if (command == null)
            return $"Command '{commandId}' not found";

        var usage = new StringBuilder();
        usage.Append(CultureInfo.InvariantCulture, $"artstudio execute {command.CommandId}");

        if (command.Parameters?.Count > 0)
        {
            var requiredParams = command.Parameters.Values.Where(p => p.IsRequired);
            foreach (var param in requiredParams.OrderBy(p => p.Name))
            {
                usage.Append(CultureInfo.InvariantCulture, $" --{param.Name} <{param.Type?.Name?.ToUpperInvariant() ?? "value"}>");
            }

            var optionalParams = command.Parameters.Values.Where(p => !p.IsRequired);
            if (optionalParams.Any())
            {
                usage.Append(" [options]");
            }
        }

        return usage.ToString();
    }
}
