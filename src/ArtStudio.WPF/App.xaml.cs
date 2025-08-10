using System.Windows;
using ArtStudio.Core.Interfaces;
using ArtStudio.Core.Services;
using ArtStudio.WPF.ViewModels;
using ArtStudio.WPF.Services;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ArtStudio.WPF;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;
    private IHost? _host;

    public App()
    {
        try
        {
            LoggingService.Initialize();
            LoggingService.LogInfo("App constructor called, about to call InitializeComponent");
            InitializeComponent();
            LoggingService.LogInfo("InitializeComponent completed successfully");
        }
        catch (Exception ex)
        {
            LoggingService.LogCritical(ex, "Critical error during App constructor or XAML parsing");

            var errorMessage = $"Critical XAML parsing error: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorMessage += $"\n\nInner Exception: {ex.InnerException.Message}";
            }

            // Log detailed exception to file and debug console
            var fullDetails = $"Exception Type: {ex.GetType().FullName}\n" +
                             $"Message: {ex.Message}\n" +
                             $"Stack Trace: {ex.StackTrace}";

            if (ex.InnerException != null)
            {
                fullDetails += $"\n\nInner Exception Type: {ex.InnerException.GetType().FullName}\n" +
                              $"Inner Exception Message: {ex.InnerException.Message}\n" +
                              $"Inner Exception Stack Trace: {ex.InnerException.StackTrace}";
            }

            LoggingService.LogCritical(fullDetails);

            MessageBox.Show(errorMessage, "XAML Parsing Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(1);
        }
    }
    protected override async void OnStartup(StartupEventArgs e)
    {
        // Initialize logging system first
        LoggingService.Initialize();
        LoggingService.LogInfo("ArtStudio application starting up");

        // Set up global exception handlers
        SetupExceptionHandling();

        try
        {
            LoggingService.LogInfo("Calling base.OnStartup");
            base.OnStartup(e);
            LoggingService.LogInfo("base.OnStartup completed successfully");
        }
        catch (Exception ex)
        {
            LoggingService.LogCritical(ex, "Exception occurred during base.OnStartup");
            throw;
        }

        try
        {
            LoggingService.LogInfo("Setting up dependency injection");

            // Create and configure the host
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.ConfigureServices();
                })
                .Build();

            // Start the host
            await _host.StartAsync();
            _serviceProvider = _host.Services;

            LoggingService.LogInfo("Dependency injection configured successfully");

            // Initialize services
            await ServiceConfiguration.InitializeServicesAsync(_serviceProvider, this);

            LoggingService.LogInfo("Services initialized successfully");

            // Create and show main window using DI
            LoggingService.LogInfo("Creating main window");
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();

            LoggingService.LogInfo("Showing main window");
            mainWindow.Show();

            LoggingService.LogInfo("ArtStudio application startup completed successfully");
        }
        catch (Exception ex)
        {
            LoggingService.LogCritical(ex, "Critical error during application startup");

            var errorMessage = $"Failed to start ArtStudio: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorMessage += $"\n\nInner Exception: {ex.InnerException.Message}";
            }

            MessageBox.Show(errorMessage, "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            LoggingService.LogInfo("Application shutting down due to startup error");
            Shutdown(1);
        }
    }

    private void SetupExceptionHandling()
    {
        // Handle unhandled exceptions in the main UI thread
        DispatcherUnhandledException += (sender, e) =>
        {
            LoggingService.LogCritical(e.Exception, "Unhandled dispatcher exception occurred");

            var errorMessage = "An unexpected error occurred in the application.";
            if (System.Diagnostics.Debugger.IsAttached)
            {
                errorMessage += $"\n\nException: {e.Exception.Message}";
                if (e.Exception.InnerException != null)
                {
                    errorMessage += $"\nInner Exception: {e.Exception.InnerException.Message}";
                }
            }

            var result = MessageBox.Show(
                $"{errorMessage}\n\nWould you like to continue running the application?",
                "Application Error",
                MessageBoxButton.YesNo,
                MessageBoxImage.Error);

            if (result == MessageBoxResult.Yes)
            {
                LoggingService.LogInfo("User chose to continue after unhandled exception");
                e.Handled = true;
            }
            else
            {
                LoggingService.LogInfo("User chose to exit after unhandled exception");
                Shutdown(1);
            }
        };

        // Handle unhandled exceptions in background threads
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                LoggingService.LogCritical(exception, "Unhandled AppDomain exception occurred");
            }
            else
            {
                LoggingService.LogCritical($"Unhandled AppDomain exception: {e.ExceptionObject}");
            }

            LoggingService.LogInfo("Application terminating due to unhandled AppDomain exception");
        };

        LoggingService.LogInfo("Global exception handling configured");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        LoggingService.LogInfo($"Application exiting with code: {e.ApplicationExitCode}");

        // Dispose of the host
        _host?.Dispose();

        LoggingService.Shutdown();
        base.OnExit(e);
    }
}
