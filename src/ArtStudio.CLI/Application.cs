using System.CommandLine;
using ArtStudio.CLI.Commands;
using Microsoft.Extensions.Logging;

namespace ArtStudio.CLI;

/// <summary>
/// Main CLI application coordinator
/// </summary>
public class Application
{
    private readonly RootCommandBuilder _rootCommandBuilder;
    private readonly ILogger<Application> _logger;

    // LoggerMessage delegates for high-performance logging
    private static readonly Action<ILogger, int, Exception?> LogApplicationStarting =
        LoggerMessage.Define<int>(LogLevel.Debug, new EventId(2001, nameof(LogApplicationStarting)),
            "Starting ArtStudio CLI application with {ArgumentCount} arguments");

    private static readonly Action<ILogger, int, Exception?> LogApplicationCompleted =
        LoggerMessage.Define<int>(LogLevel.Debug, new EventId(2002, nameof(LogApplicationCompleted)),
            "CLI application completed with exit code {ExitCode}");

    private static readonly Action<ILogger, Exception?> LogApplicationError =
        LoggerMessage.Define(LogLevel.Error, new EventId(2003, nameof(LogApplicationError)),
            "Error running CLI application");

    /// <summary>
    /// Initialize the CLI application
    /// </summary>
    public Application(
        RootCommandBuilder rootCommandBuilder,
        ILogger<Application> logger)
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
        ArgumentNullException.ThrowIfNull(args);

        LogApplicationStarting(_logger, args.Length, null);

        try
        {
            // Build the root command with all subcommands
            var rootCommand = _rootCommandBuilder.Build();

            // Execute the command line
            var result = await rootCommand.InvokeAsync(args).ConfigureAwait(false);

            LogApplicationCompleted(_logger, result, null);
            return result;
        }
#pragma warning disable CA1031 // Do not catch general exception types
        // Intentionally catching all exceptions at application entry point to ensure graceful shutdown
        // and appropriate exit codes for CLI tool
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            LogApplicationError(_logger, ex);
            return 1;
        }
    }
}
