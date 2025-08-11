using System.CommandLine;
using ArtStudio.CLI.Services;

namespace ArtStudio.CLI.Commands;

/// <summary>
/// Builds the help command for showing detailed help information
/// </summary>
public class HelpCommandBuilder
{
    private readonly HelpProvider _helpProvider;

    /// <summary>
    /// Initialize the help command builder
    /// </summary>
    public HelpCommandBuilder(HelpProvider helpProvider)
    {
        _helpProvider = helpProvider ?? throw new ArgumentNullException(nameof(helpProvider));
    }

    /// <summary>
    /// Build the help command
    /// </summary>
    public Command Build()
    {
        var helpCommand = new Command("help", "Show help information");

        // Command ID argument (optional)
        var commandIdArgument = new Argument<string?>(
            name: "command-id",
            description: "The ID of the command to get help for")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        helpCommand.AddArgument(commandIdArgument);

        // Usage option
        var usageOption = new Option<bool>(
            aliases: new[] { "--usage", "-u" },
            description: "Show only usage syntax");
        helpCommand.AddOption(usageOption);

        // Set handler
        helpCommand.SetHandler((commandId, usage) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(commandId))
                {
                    // Show general application help
                    var appHelp = _helpProvider.GetApplicationHelp();
                    Console.WriteLine(appHelp);
                }
                else
                {
                    if (usage)
                    {
                        // Show only usage syntax
                        var usageText = _helpProvider.GetCommandUsage(commandId);
                        Console.WriteLine(usageText);
                    }
                    else
                    {
                        // Show detailed command help
                        var commandHelp = _helpProvider.GetCommandHelp(commandId);
                        Console.WriteLine(commandHelp);
                    }
                }

                Environment.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        },
        commandIdArgument,
        usageOption);

        return helpCommand;
    }
}
