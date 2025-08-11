using System.CommandLine;
using ArtStudio.CLI.Services;
using ArtStudio.Core;

namespace ArtStudio.CLI.Commands;

/// <summary>
/// Builds the list command for showing available commands
/// </summary>
public class ListCommandBuilder
{
    private static readonly string[] CategoryAliases = ["--category", "-c"];
    private static readonly string[] EnabledOnlyAliases = ["--enabled-only", "-e"];
    private static readonly string[] DetailsAliases = ["--details", "-d"];
    private static readonly string[] FormatAliases = ["--format", "-f"];

    private readonly ICommandRegistry _commandRegistry;
    private readonly OutputFormatter _outputFormatter;

    /// <summary>
    /// Initialize the list command builder
    /// </summary>
    public ListCommandBuilder(
        ICommandRegistry commandRegistry,
        OutputFormatter outputFormatter)
    {
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
        _outputFormatter = outputFormatter ?? throw new ArgumentNullException(nameof(outputFormatter));
    }

    /// <summary>
    /// Build the list command
    /// </summary>
    public Command Build()
    {
        var listCommand = new Command("list", "List available commands");

        // Category option
        var categoryOption = new Option<string?>(
            aliases: CategoryAliases,
            description: "Filter commands by category");

        // Add valid categories to the option
        var validCategories = Enum.GetNames<CommandCategory>();
        categoryOption.SetDefaultValue(null);

        listCommand.AddOption(categoryOption);

        // Enabled only option
        var enabledOnlyOption = new Option<bool>(
            aliases: EnabledOnlyAliases,
            description: "Show only enabled commands");
        listCommand.AddOption(enabledOnlyOption);

        // Show details option
        var detailsOption = new Option<bool>(
            aliases: DetailsAliases,
            description: "Show detailed information for each command");
        listCommand.AddOption(detailsOption);

        // Output format option
        var formatOption = new Option<string>(
            aliases: FormatAliases,
            getDefaultValue: () => "text",
            description: "Output format (text, json, yaml, table)");
        listCommand.AddOption(formatOption);

        // Set handler
        listCommand.SetHandler((category, enabledOnly, details, format) =>
        {
            try
            {
                var commands = _commandRegistry.Commands.AsEnumerable();

                // Filter by category if specified
                if (!string.IsNullOrWhiteSpace(category))
                {
                    if (Enum.TryParse<CommandCategory>(category, true, out var categoryEnum))
                    {
                        commands = commands.Where(c => c.Category == categoryEnum);
                    }
                    else
                    {
                        Console.Error.WriteLine($"Error: Invalid category '{category}'. Valid categories: {string.Join(", ", validCategories)}");
                        Environment.ExitCode = 1;
                        return;
                    }
                }

                // Filter by enabled status if requested
                if (enabledOnly)
                {
                    commands = commands.Where(c => c.IsEnabled);
                }

                var commandList = commands.ToList();

                if (details)
                {
                    // Show detailed information
                    foreach (var command in commandList.OrderBy(c => c.Category).ThenBy(c => c.CommandId))
                    {
                        Console.WriteLine($"Command: {command.CommandId}");
                        Console.WriteLine($"  Name: {command.DisplayName}");
                        Console.WriteLine($"  Description: {command.Description}");
                        Console.WriteLine($"  Category: {command.Category}");
                        Console.WriteLine($"  Priority: {command.Priority}");
                        Console.WriteLine($"  Enabled: {command.IsEnabled}");
                        Console.WriteLine($"  Visible: {command.IsVisible}");

                        if (!string.IsNullOrWhiteSpace(command.KeyboardShortcut))
                            Console.WriteLine($"  Shortcut: {command.KeyboardShortcut}");

                        if (command.Parameters?.Count > 0)
                        {
                            Console.WriteLine($"  Parameters: {command.Parameters.Count}");
                            foreach (var param in command.Parameters.Values)
                            {
                                Console.WriteLine($"    --{param.Name} ({param.Type?.Name}) {(param.IsRequired ? "[required]" : "[optional]")}");
                            }
                        }
                        Console.WriteLine();
                    }
                }
                else
                {
                    // Show summary list
                    var outputFormat = Enum.TryParse<OutputFormatter.OutputFormat>(format, true, out var fmt)
                        ? fmt
                        : OutputFormatter.OutputFormat.Text;

                    var output = _outputFormatter.FormatCommandList(commandList, outputFormat);
                    Console.WriteLine(output);
                }

                Environment.ExitCode = 0;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            // Intentionally catching all exceptions in CLI command handler to provide
            // user-friendly error messages and appropriate exit codes
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        },
        categoryOption,
        enabledOnlyOption,
        detailsOption,
        formatOption);

        return listCommand;
    }
}
