using System.CommandLine;
using ArtStudio.CLI.Commands;
using Microsoft.Extensions.Logging;

namespace ArtStudio.CLI;

/// <summary>
/// Main CLI application coordinator
/// </summary>
public class CliApplication
{
    private readonly RootCommandBuilder _rootCommandBuilder;
    private readonly ILogger<CliApplication> _logger;

    /// <summary>
    /// Initialize the CLI application
    /// </summary>
    public CliApplication(
        RootCommandBuilder rootCommandBuilder,
        ILogger<CliApplication> logger)
    {
        _rootCommandBuilder = rootCommandBuilder ?? throw new ArgumentNullException(nameof(rootCommandBuilder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Run the CLI application
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Exit code</returns>
    public async Task<int> RunAsync(string[] args)
    {
        _logger.LogDebug("Starting ArtStudio CLI application with {ArgumentCount} arguments", args.Length);

        try
        {
            // Build the root command with all subcommands
            var rootCommand = _rootCommandBuilder.Build();

            // Execute the command line
            var result = await rootCommand.InvokeAsync(args);

            _logger.LogDebug("CLI application completed with exit code {ExitCode}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running CLI application");
            return 1;
        }
    }
}
