using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ArtStudio.Core;
using ArtStudio.Core.Services;
using ArtStudio.WPF.ViewModels;
using ArtStudio.WPF.Services;
using System.Windows;
using System.IO;
using System.Linq;

namespace ArtStudio.WPF.Services;

/// <summary>
/// Configures dependency injection services for the ArtStudio application
/// </summary>
public static class ServiceConfiguration
{
    /// <summary>
    /// Configures all services for the application
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <returns>The configured service collection</returns>
    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        // Configure logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
            builder.AddDebug();
        });

        // Core services (singletons)
        services.AddSingleton<IConfigurationManager, ConfigurationManager>();
        services.AddSingleton<IThemeManager, ThemeManager>();
        services.AddSingleton<IPluginManager, PluginManager>();
        services.AddSingleton<IWorkspaceManager, WorkspaceManager>();
        services.AddSingleton<IWorkspaceLayoutManager, WorkspaceLayoutManager>();

        // Application services (singletons)
        services.AddSingleton<IEditorService, EditorService>();
        services.AddSingleton<DockingService>();

        // ViewModels (transient - new instance each time)
        services.AddTransient<ToolPaletteViewModel>();
        services.AddTransient<LayerPaletteViewModel>();
        services.AddTransient<EditorViewModel>();
        services.AddTransient<WorkspaceViewModel>();
        services.AddTransient<MainViewModel>();

        // Main Window (transient)
        services.AddTransient<MainWindow>();

        return services;
    }

    /// <summary>
    /// Initializes services that require startup configuration
    /// </summary>
    /// <param name="serviceProvider">The configured service provider</param>
    /// <param name="app">The WPF application instance</param>
    public static async Task InitializeServicesAsync(IServiceProvider serviceProvider, App app)
    {
        // Skip theme manager initialization here - it will be done after MainWindow creation

        // Initialize workspace manager
        var workspaceManager = serviceProvider.GetRequiredService<IWorkspaceManager>();
        await workspaceManager.InitializeAsync();

        // Initialize plugin manager
        var pluginManager = serviceProvider.GetRequiredService<IPluginManager>();

        // Load plugins from default directories
        var pluginPaths = GetDefaultPluginPaths();
        var existingPaths = pluginPaths.Where(Directory.Exists).ToArray();

        if (existingPaths.Length > 0)
        {
            await pluginManager.LoadPluginsAsync(existingPaths);
        }

        // Log initialization complete
        var logger = serviceProvider.GetRequiredService<ILogger<Application>>();
        logger.LogInformation("Services initialized successfully");
    }

    /// <summary>
    /// Initialize the theme manager after the main window is created
    /// </summary>
    /// <param name="serviceProvider">The configured service provider</param>
    /// <param name="app">The WPF application instance</param>
    public static void InitializeThemeManager(IServiceProvider serviceProvider, App app)
    {
        // Initialize theme manager after MainWindow creation to avoid resource conflicts
        var themeManager = serviceProvider.GetRequiredService<IThemeManager>();
        themeManager.Initialize(app);
    }

    /// <summary>
    /// Gets the default plugin search paths
    /// </summary>
    /// <returns>Array of plugin directory paths</returns>
    private static string[] GetDefaultPluginPaths()
    {
        var appDirectory = Path.GetDirectoryName(AppContext.BaseDirectory) ?? string.Empty;

        return new[]
        {
            Path.Combine(appDirectory, "Plugins"),
            Path.Combine(appDirectory, "Extensions"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ArtStudio", "Plugins"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ArtStudio", "Plugins")
        };
    }
}
