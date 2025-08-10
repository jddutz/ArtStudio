# Theme Configuration System

## Overview

ArtStudio now includes a comprehensive theme management system that allows users to customize the application's appearance through configuration files and UI settings.

## Features

- **Dynamic Theme Switching**: Switch between Light, Dark, and High Contrast themes
- **Material Design Integration**: Full support for Material Design themes with customizable primary and secondary colors
- **Configuration-Based**: All theme settings are stored in a JSON configuration file
- **System Theme Detection**: Automatically follow the Windows system theme setting
- **Live Preview**: See theme changes immediately in the UI

## Configuration File

The configuration is stored in `%APPDATA%\ArtStudio\config.json`. Here's an example:

```json
{
  "CurrentTheme": "Dark",
  "MaterialDesignBaseTheme": "Dark",
  "MaterialDesignPrimaryColor": "DeepPurple",
  "MaterialDesignSecondaryColor": "Lime",
  "UseSystemTheme": false,
  "WindowWidth": 1200.0,
  "WindowHeight": 800.0,
  "WindowMaximized": false,
  "RecentFiles": []
}
```

## Theme Options

### Available Themes

- **Light**: Light theme with bright colors
- **Dark**: Dark theme optimized for low-light environments
- **HighContrast**: High contrast theme for accessibility

### Material Design Colors

Both primary and secondary colors can be set to any of these Material Design colors:

- Red, Pink, Purple, DeepPurple, Indigo, Blue, LightBlue
- Cyan, Teal, Green, LightGreen, Lime, Yellow, Amber
- Orange, DeepOrange, Brown, Grey, BlueGrey

## Usage

### Programmatically

```csharp
// Get services from dependency injection
var configManager = services.GetService<IConfigurationManager>();
var themeManager = services.GetService<IThemeManager>();

// Change theme
configManager.CurrentTheme = "Light";
themeManager.ApplyTheme("Light");

// Enable system theme following
configManager.UseSystemTheme = true;
themeManager.ApplySystemTheme();
```

### Through UI

1. Access theme settings through the application menu
2. Choose between manual theme selection or system theme following
3. Customize Material Design colors
4. Preview changes live

## Architecture

### Core Components

1. **IConfigurationManager**: Manages application configuration including theme settings
2. **IThemeManager**: Handles theme application and Material Design integration
3. **ThemeSettingsViewModel**: ViewModel for the theme settings UI
4. **ThemeSettingsView**: WPF UserControl for theme configuration

### Theme Files

Custom theme resource dictionaries are located in:

- `Themes/LightTheme.xaml`
- `Themes/DarkTheme.xaml`
- `Themes/HighContrastTheme.xaml`

Each theme file defines color brushes and styles specific to that theme.

## Extending the System

### Adding New Themes

1. Create a new ResourceDictionary in the `Themes` folder
2. Define all required color brushes (see existing themes for reference)
3. Add the theme name to the `AvailableThemes` array in `ThemeManager`
4. Update the theme selection UI

### Adding New Configuration Options

1. Add properties to `IConfigurationManager`
2. Update `ConfigurationManager` implementation
3. Add UI elements to `ThemeSettingsView`
4. Bind to properties in `ThemeSettingsViewModel`

## Best Practices

1. Always use theme-aware color resources (e.g., `ArtStudio.Background`) instead of hardcoded colors
2. Test UI elements in all available themes
3. Ensure high contrast themes meet accessibility guidelines
4. Use the configuration system for any user-customizable settings

## Troubleshooting

### Theme Not Applying

- Check that theme resource files exist and are properly referenced
- Verify MaterialDesign packages are correctly installed
- Ensure configuration file is not corrupted

### Configuration Not Persisting

- Check write permissions to `%APPDATA%\ArtStudio\` folder
- Verify configuration file is not locked by another process
