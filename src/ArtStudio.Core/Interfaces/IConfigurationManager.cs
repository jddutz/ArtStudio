using System;

namespace ArtStudio.Core;

/// <summary>
/// Interface for managing application configuration settings
/// </summary>
public interface IConfigurationManager
{
    /// <summary>
    /// Event raised when configuration changes
    /// </summary>
    event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;

    /// <summary>
    /// Gets the current theme name
    /// </summary>
    string CurrentTheme { get; set; }

    /// <summary>
    /// Gets the Material Design base theme (Light/Dark)
    /// </summary>
    string MaterialDesignBaseTheme { get; set; }

    /// <summary>
    /// Gets the Material Design primary color
    /// </summary>
    string MaterialDesignPrimaryColor { get; set; }

    /// <summary>
    /// Gets the Material Design secondary color
    /// </summary>
    string MaterialDesignSecondaryColor { get; set; }

    /// <summary>
    /// Gets whether to use system theme detection
    /// </summary>
    bool UseSystemTheme { get; set; }

    /// <summary>
    /// Loads configuration from the configuration file
    /// </summary>
    void LoadConfiguration();

    /// <summary>
    /// Saves current configuration to the configuration file
    /// </summary>
    void SaveConfiguration();

    /// <summary>
    /// Gets a configuration value by key
    /// </summary>
    T GetValue<T>(string key, T? defaultValue = default);

    /// <summary>
    /// Sets a configuration value by key
    /// </summary>
    void SetValue<T>(string key, T value);

    /// <summary>
    /// Gets all available theme names
    /// </summary>
    string[] GetAvailableThemes();
}
