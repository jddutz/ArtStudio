using Microsoft.Extensions.Logging;
using Serilog;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.IO;

namespace ArtStudio.WPF.Services;

/// <summary>
/// Comprehensive logging service with SQLite journal and debug console output
/// </summary>
public static class LoggingService
{
    private static Microsoft.Extensions.Logging.ILogger? _logger;
    private static readonly object _lock = new();

    /// <summary>
    /// Initialize the logging system with SQLite journal and debug console
    /// </summary>
    public static void Initialize()
    {
        lock (_lock)
        {
            if (_logger != null) return;

            // Create logs directory if it doesn't exist
            var logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logsDir);

            // Configure Serilog
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.WithProperty("Application", "ArtStudio")
                .Enrich.WithProperty("Version", GetApplicationVersion())
                .WriteTo.SQLite(
                    sqliteDbPath: Path.Combine(logsDir, "journal.db"),
                    tableName: "Logs",
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
                .WriteTo.File(
                    path: Path.Combine(logsDir, "artstudio-.log"),
                    rollingInterval: Serilog.RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Debug(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

            // Add console output in debug mode
            if (Debugger.IsAttached)
            {
                loggerConfig.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
            }

            Log.Logger = loggerConfig.CreateLogger();

            // Create Microsoft.Extensions.Logging wrapper
            using var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddSerilog(dispose: true));

            _logger = loggerFactory.CreateLogger("ArtStudio");

            // Log initialization
            LogInfo("Logging system initialized");
            LogInfo($"Application Version: {GetApplicationVersion()}");
            LogInfo($"CLR Version: {Environment.Version}");
            LogInfo($"OS Version: {Environment.OSVersion}");
            LogInfo($"Working Directory: {Environment.CurrentDirectory}");
            LogInfo($"Logs Directory: {logsDir}");
        }
    }

    private static string GetApplicationVersion()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    public static void LogDebug(string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0)
    {
        EnsureInitialized();
        var context = $"{Path.GetFileNameWithoutExtension(filePath)}.{memberName}:{lineNumber}";
        _logger?.LogDebug("[{Context}] {Message}", context, message);
    }

    public static void LogInfo(string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0)
    {
        EnsureInitialized();
        var context = $"{Path.GetFileNameWithoutExtension(filePath)}.{memberName}:{lineNumber}";
        _logger?.LogInformation("[{Context}] {Message}", context, message);
    }

    public static void LogWarning(string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0)
    {
        EnsureInitialized();
        var context = $"{Path.GetFileNameWithoutExtension(filePath)}.{memberName}:{lineNumber}";
        _logger?.LogWarning("[{Context}] {Message}", context, message);
    }

    public static void LogError(string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0)
    {
        EnsureInitialized();
        var context = $"{Path.GetFileNameWithoutExtension(filePath)}.{memberName}:{lineNumber}";
        _logger?.LogError("[{Context}] {Message}", context, message);
    }

    public static void LogError(Exception exception, string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0)
    {
        EnsureInitialized();
        var context = $"{Path.GetFileNameWithoutExtension(filePath)}.{memberName}:{lineNumber}";

        _logger?.LogError(exception, "[{Context}] {Message}", context, message);

        // In debug mode, also output detailed exception information to debug console
        if (Debugger.IsAttached)
        {
            Debug.WriteLine($"=== EXCEPTION DETAILS ===");
            Debug.WriteLine($"Context: {context}");
            Debug.WriteLine($"Message: {message}");
            Debug.WriteLine($"Exception Type: {exception.GetType().FullName}");
            Debug.WriteLine($"Exception Message: {exception.Message}");

            if (exception.InnerException != null)
            {
                Debug.WriteLine($"Inner Exception: {exception.InnerException.GetType().FullName}");
                Debug.WriteLine($"Inner Exception Message: {exception.InnerException.Message}");
            }

            Debug.WriteLine($"Stack Trace:");
            Debug.WriteLine(exception.StackTrace);
            Debug.WriteLine($"=== END EXCEPTION DETAILS ===");
        }
    }

    public static void LogCritical(string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0)
    {
        EnsureInitialized();
        var context = $"{Path.GetFileNameWithoutExtension(filePath)}.{memberName}:{lineNumber}";
        _logger?.LogCritical("[{Context}] {Message}", context, message);
    }

    public static void LogCritical(Exception exception, string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0)
    {
        EnsureInitialized();
        var context = $"{Path.GetFileNameWithoutExtension(filePath)}.{memberName}:{lineNumber}";

        _logger?.LogCritical(exception, "[{Context}] {Message}", context, message);

        // In debug mode, also output detailed exception information to debug console
        if (Debugger.IsAttached)
        {
            Debug.WriteLine($"=== CRITICAL EXCEPTION ===");
            Debug.WriteLine($"Context: {context}");
            Debug.WriteLine($"Message: {message}");
            Debug.WriteLine($"Exception Type: {exception.GetType().FullName}");
            Debug.WriteLine($"Exception Message: {exception.Message}");

            if (exception.InnerException != null)
            {
                Debug.WriteLine($"Inner Exception: {exception.InnerException.GetType().FullName}");
                Debug.WriteLine($"Inner Exception Message: {exception.InnerException.Message}");
            }

            Debug.WriteLine($"Stack Trace:");
            Debug.WriteLine(exception.StackTrace);
            Debug.WriteLine($"=== END CRITICAL EXCEPTION ===");
        }
    }

    private static void EnsureInitialized()
    {
        if (_logger == null)
        {
            Initialize();
        }
    }

    public static void Shutdown()
    {
        LogInfo("Shutting down logging system");
        Log.CloseAndFlush();
    }
}
