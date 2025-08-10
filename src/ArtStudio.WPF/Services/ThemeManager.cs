using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ArtStudio.Core.Interfaces;
using MaterialDesignThemes.Wpf;
using MaterialDesignColors;
using Microsoft.Win32;
using CoreThemeManager = ArtStudio.Core.Interfaces.IThemeManager;
using CoreThemeChangedEventArgs = ArtStudio.Core.Interfaces.ThemeChangedEventArgs;

namespace ArtStudio.WPF.Services;

/// <summary>
/// WPF implementation of theme management
/// </summary>
public class ThemeManager : CoreThemeManager
{
    private Application? _application;
    private readonly IConfigurationManager _configurationManager;
    private string _currentTheme = "Dark";

    public event EventHandler<CoreThemeChangedEventArgs>? ThemeChanged;

    public string CurrentTheme => _currentTheme;

    public string[] AvailableThemes => new[] { "Light", "Dark", "HighContrast" };

    public ThemeManager(IConfigurationManager configurationManager)
    {
        _configurationManager = configurationManager;
        _configurationManager.ConfigurationChanged += OnConfigurationChanged;
    }

    public void Initialize(object application)
    {
        if (application is Application wpfApp)
        {
            _application = wpfApp;

            // Apply the theme from configuration
            var themeName = _configurationManager.CurrentTheme;
            ApplyTheme(themeName);

            // Set up system theme monitoring if enabled
            if (_configurationManager.UseSystemTheme)
            {
                MonitorSystemTheme();
            }
        }
    }

    public void ApplyTheme(string themeName)
    {
        if (_application == null || !ThemeExists(themeName))
            return;

        var previousTheme = _currentTheme;
        _currentTheme = themeName;

        try
        {
            // Clear existing theme resources first
            ClearThemeResources();

            // Apply Material Design theme
            ApplyMaterialDesignTheme(themeName);

            // Apply custom theme resources
            ApplyCustomThemeResources(themeName);

            // Update configuration
            _configurationManager.CurrentTheme = themeName;

            ThemeChanged?.Invoke(this, new CoreThemeChangedEventArgs(previousTheme, themeName));
        }
        catch (Exception)
        {
            // If theme application fails, revert to previous theme
            _currentTheme = previousTheme;
            throw;
        }
    }

    public void ApplySystemTheme()
    {
        var systemUsesLightTheme = IsSystemLightTheme();
        var themeName = systemUsesLightTheme ? "Light" : "Dark";
        ApplyTheme(themeName);
    }

    public bool ThemeExists(string themeName)
    {
        return AvailableThemes.Contains(themeName, StringComparer.OrdinalIgnoreCase);
    }

    private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        if (e.Key == "UseSystemTheme" && e.NewValue is bool useSystemTheme && useSystemTheme)
        {
            MonitorSystemTheme();
            ApplySystemTheme();
        }
        else if (e.Key == "CurrentTheme" && e.NewValue is string themeName && themeName != _currentTheme)
        {
            ApplyTheme(themeName);
        }
    }

    private void ClearThemeResources()
    {
        if (_application?.Resources == null)
            return;

        // Remove existing theme dictionaries
        var resourcesToRemove = _application.Resources.MergedDictionaries
            .Where(rd => IsThemeResourceDictionary(rd))
            .ToList();

        foreach (var resource in resourcesToRemove)
        {
            _application.Resources.MergedDictionaries.Remove(resource);
        }
    }

    private void ApplyMaterialDesignTheme(string themeName)
    {
        if (_application?.Resources == null)
            return;

        // Determine Material Design base theme
        var baseTheme = themeName switch
        {
            "Light" => BaseTheme.Light,
            "Dark" => BaseTheme.Dark,
            "HighContrast" => BaseTheme.Dark, // Use dark as base for high contrast
            _ => BaseTheme.Dark
        };

        // Get colors from configuration
        var primaryColor = Enum.TryParse<PrimaryColor>(_configurationManager.MaterialDesignPrimaryColor, out var primary)
            ? primary
            : PrimaryColor.DeepPurple;

        var secondaryColor = Enum.TryParse<SecondaryColor>(_configurationManager.MaterialDesignSecondaryColor, out var secondary)
            ? secondary
            : SecondaryColor.Lime;

        // Create and apply the bundled theme
        var bundledTheme = new BundledTheme
        {
            BaseTheme = baseTheme,
            PrimaryColor = primaryColor,
            SecondaryColor = secondaryColor
        };

        _application.Resources.MergedDictionaries.Insert(0, bundledTheme);

        // Add Material Design defaults
        _application.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml")
        });
    }

    private void ApplyCustomThemeResources(string themeName)
    {
        if (_application?.Resources == null)
            return;

        // Apply custom theme resource dictionary
        var themeUri = $"pack://application:,,,/ArtStudio.WPF;component/Themes/{themeName}Theme.xaml";

        try
        {
            var themeResourceDictionary = new ResourceDictionary
            {
                Source = new Uri(themeUri)
            };
            _application.Resources.MergedDictionaries.Add(themeResourceDictionary);
        }
        catch (Exception)
        {
            // If custom theme doesn't exist, continue without it
        }
    }

    private static bool IsThemeResourceDictionary(ResourceDictionary resourceDictionary)
    {
        if (resourceDictionary.Source == null)
            return false;

        var sourceString = resourceDictionary.Source.ToString();
        return sourceString.Contains("MaterialDesignThemes.Wpf") ||
               sourceString.Contains("/Themes/") ||
               resourceDictionary is BundledTheme;
    }

    private static bool IsSystemLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int intValue && intValue == 1;
        }
        catch
        {
            return false; // Default to dark theme if we can't determine
        }
    }

    private void MonitorSystemTheme()
    {
        // This is a simplified implementation
        // In a real application, you might want to use SystemEvents.UserPreferenceChanged
        // or implement a more sophisticated monitoring system
        try
        {
            Microsoft.Win32.SystemEvents.UserPreferenceChanged += (sender, e) =>
            {
                if (e.Category == Microsoft.Win32.UserPreferenceCategory.General && _configurationManager.UseSystemTheme)
                {
                    _application?.Dispatcher.BeginInvoke(() => ApplySystemTheme());
                }
            };
        }
        catch
        {
            // System events monitoring is not critical, so we can continue without it
        }
    }
}
