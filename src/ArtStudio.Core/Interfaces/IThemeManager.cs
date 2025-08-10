namespace ArtStudio.Core;

/// <summary>
/// Interface for managing application themes
/// </summary>
public interface IThemeManager
{
    /// <summary>
    /// Event raised when theme changes
    /// </summary>
    event EventHandler<ThemeChangedEventArgs> ThemeChanged;

    /// <summary>
    /// Gets the current theme name
    /// </summary>
    string CurrentTheme { get; }

    /// <summary>
    /// Gets all available theme names
    /// </summary>
    string[] AvailableThemes { get; }

    /// <summary>
    /// Applies a theme by name
    /// </summary>
    void ApplyTheme(string themeName);

    /// <summary>
    /// Applies the system theme if available
    /// </summary>
    void ApplySystemTheme();

    /// <summary>
    /// Checks if a theme exists
    /// </summary>
    bool ThemeExists(string themeName);

    /// <summary>
    /// Initializes the theme manager with the application
    /// </summary>
    void Initialize(object application);
}
