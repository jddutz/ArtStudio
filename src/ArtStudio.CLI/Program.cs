using System.CommandLine;
using ArtStudio.CLI.Commands;
using ArtStudio.CLI.Services;
using ArtStudio.Core;
using ArtStudio.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ArtStudio.CLI;

/// <summary>
/// Main entry point for ArtStudio CLI application
/// </summary>
internal class Program
{
    /// <summary>
    /// Main entry point
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Exit code</returns>
    public static async Task<int> Main(string[] args)
    {
        // Create host builder for dependency injection
        var hostBuilder = Host.CreateDefaultBuilder(args)
            .ConfigureServices(ConfigureServices)
            .ConfigureLogging(ConfigureLogging);

        // Build and run the CLI application
        using var host = hostBuilder.Build();

        try
        {
            var cliApp = host.Services.GetRequiredService<CliApplication>();
            return await cliApp.RunAsync(args);
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetService<ILogger<Program>>();
            logger?.LogError(ex, "Unhandled exception in CLI application");

            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Configure services for dependency injection
    /// </summary>
    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        // Core services
        services.AddSingleton<ICommandRegistry, CommandRegistry>();
        services.AddSingleton<IConfigurationManager, ConfigurationManager>();
        services.AddSingleton<IEditorService, HeadlessEditorService>();

        // CLI-specific services
        services.AddSingleton<CliApplication>();
        services.AddSingleton<CommandExecutor>();
        services.AddSingleton<BatchProcessor>();
        services.AddSingleton<ArgumentParser>();
        services.AddSingleton<HelpProvider>();
        services.AddSingleton<OutputFormatter>();

        // Command builders
        services.AddSingleton<RootCommandBuilder>();
        services.AddSingleton<ExecuteCommandBuilder>();
        services.AddSingleton<BatchCommandBuilder>();
        services.AddSingleton<ListCommandBuilder>();
        services.AddSingleton<HelpCommandBuilder>();
    }

    /// <summary>
    /// Configure logging for the CLI application
    /// </summary>
    private static void ConfigureLogging(HostBuilderContext context, ILoggingBuilder logging)
    {
        logging.ClearProviders();
        logging.AddSimpleConsole(options =>
        {
            options.IncludeScopes = false;
            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
        });

        // Set log level based on verbosity
        var logLevel = context.Configuration["LogLevel"] switch
        {
            "Trace" => LogLevel.Trace,
            "Debug" => LogLevel.Debug,
            "Information" => LogLevel.Information,
            "Warning" => LogLevel.Warning,
            "Error" => LogLevel.Error,
            "Critical" => LogLevel.Critical,
            _ => LogLevel.Information
        };

        logging.SetMinimumLevel(logLevel);
    }
}
