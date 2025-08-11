using System.CommandLine;
using ArtStudio.CLI.Services;

namespace ArtStudio.CLI.Commands;

/// <summary>
/// Builds the root command for the CLI application
/// </summary>
public class RootCommandBuilder
{
    private static readonly string[] VerboseAliases = ["--verbose", "-v"];
    private static readonly string[] QuietAliases = ["--quiet", "-q"];
    private static readonly string[] ConfigAliases = ["--config", "-c"];
    private static readonly string[] FormatAliases = ["--format", "-f"];

    private readonly ExecuteCommandBuilder _executeCommandBuilder;
    private readonly BatchCommandBuilder _batchCommandBuilder;
    private readonly ListCommandBuilder _listCommandBuilder;
    private readonly HelpCommandBuilder _helpCommandBuilder;

    /// <summary>
    /// Initialize the root command builder
    /// </summary>
    public RootCommandBuilder(
        ExecuteCommandBuilder executeCommandBuilder,
        BatchCommandBuilder batchCommandBuilder,
        ListCommandBuilder listCommandBuilder,
        HelpCommandBuilder helpCommandBuilder)
    {
        _executeCommandBuilder = executeCommandBuilder ?? throw new ArgumentNullException(nameof(executeCommandBuilder));
        _batchCommandBuilder = batchCommandBuilder ?? throw new ArgumentNullException(nameof(batchCommandBuilder));
        _listCommandBuilder = listCommandBuilder ?? throw new ArgumentNullException(nameof(listCommandBuilder));
        _helpCommandBuilder = helpCommandBuilder ?? throw new ArgumentNullException(nameof(helpCommandBuilder));
    }

    /// <summary>
    /// Build the root command with all subcommands
    /// </summary>
    public RootCommand Build()
    {
        var rootCommand = new RootCommand("ArtStudio CLI - Command-line interface for ArtStudio automation");

        // Global options
        var verboseOption = new Option<bool>(
            aliases: VerboseAliases,
            description: "Enable verbose logging");

        var quietOption = new Option<bool>(
            aliases: QuietAliases,
            description: "Suppress output except errors");

        var configOption = new Option<string?>(
            aliases: ConfigAliases,
            description: "Specify configuration file path");

        var outputFormatOption = new Option<string>(
            aliases: FormatAliases,
            getDefaultValue: () => "text",
            description: "Output format (text, json, yaml, table)");

        // Add global options to root command
        rootCommand.AddGlobalOption(verboseOption);
        rootCommand.AddGlobalOption(quietOption);
        rootCommand.AddGlobalOption(configOption);
        rootCommand.AddGlobalOption(outputFormatOption);

        // Add subcommands
        rootCommand.AddCommand(_executeCommandBuilder.Build());
        rootCommand.AddCommand(_batchCommandBuilder.Build());
        rootCommand.AddCommand(_listCommandBuilder.Build());
        rootCommand.AddCommand(_helpCommandBuilder.Build());

        // Add version command
        var versionCommand = new Command("version", "Show version information");
        versionCommand.SetHandler(() =>
        {
            var version = typeof(RootCommandBuilder).Assembly.GetName().Version;
            Console.WriteLine($"ArtStudio CLI version {version}");
        });
        rootCommand.AddCommand(versionCommand);

        return rootCommand;
    }
}
