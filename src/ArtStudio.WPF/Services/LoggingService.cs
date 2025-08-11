using Microsoft.Extensions.Logging;
using Serilog;
using System.Diagnostics;
using System.Globalization;
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

    // High-performance logging delegates
    private static readonly Action<Microsoft.Extensions.Logging.ILogger, string, string, Exception?> _logDebugDelegate =
        LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(1, nameof(LogDebug)), "[{Context}] {Message}");

    private static readonly Action<Microsoft.Extensions.Logging.ILogger, string, string, Exception?> _logInfoDelegate =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(2, nameof(LogInfo)), "[{Context}] {Message}");

    private static readonly Action<Microsoft.Extensions.Logging.ILogger, string, string, Exception?> _logWarningDelegate =
        LoggerMessage.Define<string, string>(LogLevel.Warning, new EventId(3, nameof(LogWarning)), "[{Context}] {Message}");

    private static readonly Action<Microsoft.Extensions.Logging.ILogger, string, string, Exception?> _logErrorDelegate =
        LoggerMessage.Define<string, string>(LogLevel.Error, new EventId(4, nameof(LogError)), "[{Context}] {Message}");

    private static readonly Action<Microsoft.Extensions.Logging.ILogger, string, string, Exception?> _logErrorWithExceptionDelegate =
        LoggerMessage.Define<string, string>(LogLevel.Error, new EventId(5, nameof(LogError)), "[{Context}] {Message}");

    private static readonly Action<Microsoft.Extensions.Logging.ILogger, string, string, Exception?> _logCriticalDelegate =
        LoggerMessage.Define<string, string>(LogLevel.Critical, new EventId(6, nameof(LogCritical)), "[{Context}] {Message}");

    private static readonly Action<Microsoft.Extensions.Logging.ILogger, string, string, Exception?> _logCriticalWithExceptionDelegate =
        LoggerMessage.Define<string, string>(LogLevel.Critical, new EventId(7, nameof(LogCritical)), "[{Context}] {Message}");

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
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
                    formatProvider: CultureInfo.InvariantCulture)
                .WriteTo.File(
                    path: Path.Combine(logsDir, "artstudio-.log"),
                    rollingInterval: Serilog.RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    formatProvider: CultureInfo.InvariantCulture)
                .WriteTo.Debug(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                    formatProvider: CultureInfo.InvariantCulture);

            // Add console output in debug mode
            if (Debugger.IsAttached)
            {
                loggerConfig.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                    formatProvider: CultureInfo.InvariantCulture);
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
#pragma warning disable CA1031 // Do not catch general exception types
        // Gracefully handle reflection errors by returning fallback value
        catch
#pragma warning restore CA1031 // Do not catch general exception types
        {
            return "Unknown";
        }
    }

    public static void LogDebug(string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0)
    {
        EnsureInitialized();
        var context = $"{Path.GetFileNameWithoutExtension(filePath)}.{memberName}:{lineNumber}";
        if (_logger != null)
        {
            _logDebugDelegate(_logger, context, message, null);
        }
    }

    public static void LogInfo(string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0)
    {
        EnsureInitialized();
        var context = $"{Path.GetFileNameWithoutExtension(filePath)}.{memberName}:{lineNumber}";
        if (_logger != null)
        {
            _logInfoDelegate(_logger, context, message, null);
        }
    }

    public static void LogWarning(string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0)
    {
        EnsureInitialized();
        var context = $"{Path.GetFileNameWithoutExtension(filePath)}.{memberName}:{lineNumber}";
        if (_logger != null)
        {
            _logWarningDelegate(_logger, context, message, null);
        }
    }

    public static void LogError(string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0)
    {
        EnsureInitialized();
        var context = $"{Path.GetFileNameWithoutExtension(filePath)}.{memberName}:{lineNumber}";
        if (_logger != null)
        {
            _logErrorDelegate(_logger, context, message, null);
        }
    }

    public static void LogError(Exception exception, string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0)
    {
        ArgumentNullException.ThrowIfNull(exception);

        EnsureInitialized();
        var context = $"{Path.GetFileNameWithoutExtension(filePath)}.{memberName}:{lineNumber}";

        if (_logger != null)
        {
            _logErrorWithExceptionDelegate(_logger, context, message, exception);
        }

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
        if (_logger != null)
        {
            _logCriticalDelegate(_logger, context, message, null);
        }
    }

    public static void LogCritical(Exception exception, string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0)
    {
        ArgumentNullException.ThrowIfNull(exception);

        EnsureInitialized();
        var context = $"{Path.GetFileNameWithoutExtension(filePath)}.{memberName}:{lineNumber}";

        if (_logger != null)
        {
            _logCriticalWithExceptionDelegate(_logger, context, message, exception);
        }

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
