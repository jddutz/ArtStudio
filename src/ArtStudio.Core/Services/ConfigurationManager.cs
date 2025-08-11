using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using ArtStudio.Core;

namespace ArtStudio.Core.Services;

/// <summary>
/// Implementation of configuration management using JSON files
/// </summary>
public class ConfigurationManager : IConfigurationManager
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _configFilePath;
    private readonly Dictionary<string, object> _configuration;
    private readonly string[] _availableThemes = { "Light", "Dark", "HighContrast" };

    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    public ConfigurationManager()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var artStudioPath = Path.Combine(appDataPath, "ArtStudio");
        Directory.CreateDirectory(artStudioPath);
        _configFilePath = Path.Combine(artStudioPath, "config.json");
        _configuration = new Dictionary<string, object>();
        LoadConfiguration();
    }

    public string CurrentTheme
    {
        get => GetValue("CurrentTheme", "Dark");
        set => SetValue("CurrentTheme", value);
    }

    public string MaterialDesignBaseTheme
    {
        get => GetValue("MaterialDesignBaseTheme", "Dark");
        set => SetValue("MaterialDesignBaseTheme", value);
    }

    public string MaterialDesignPrimaryColor
    {
        get => GetValue("MaterialDesignPrimaryColor", "DeepPurple");
        set => SetValue("MaterialDesignPrimaryColor", value);
    }

    public string MaterialDesignSecondaryColor
    {
        get => GetValue("MaterialDesignSecondaryColor", "Lime");
        set => SetValue("MaterialDesignSecondaryColor", value);
    }

    public bool UseSystemTheme
    {
        get => GetValue("UseSystemTheme", false);
        set => SetValue("UseSystemTheme", value);
    }

    public void LoadConfiguration()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                // Create default configuration
                CreateDefaultConfiguration();
                SaveConfiguration();
                return;
            }

            var json = File.ReadAllText(_configFilePath);
            var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (config != null)
            {
                _configuration.Clear();
                foreach (var kvp in config)
                {
                    _configuration[kvp.Key] = ConvertJsonElement(kvp.Value);
                }
            }
        }
        catch (IOException)
        {
            // If configuration loading fails due to IO issues, create default configuration
            CreateDefaultConfiguration();
            SaveConfiguration();
        }
        catch (JsonException)
        {
            // If configuration loading fails due to JSON issues, create default configuration
            CreateDefaultConfiguration();
            SaveConfiguration();
        }
        catch (UnauthorizedAccessException)
        {
            // If configuration loading fails due to access issues, create default configuration
            CreateDefaultConfiguration();
            SaveConfiguration();
        }
    }

    public void SaveConfiguration()
    {
        try
        {
            var json = JsonSerializer.Serialize(_configuration, SerializerOptions);
            File.WriteAllText(_configFilePath, json);
        }
        catch (IOException)
        {
            // Silently handle IO errors - configuration will be lost but app can continue
        }
        catch (UnauthorizedAccessException)
        {
            // Silently handle access errors - configuration will be lost but app can continue
        }
        catch (JsonException)
        {
            // Silently handle JSON serialization errors - configuration will be lost but app can continue
        }
    }

    public T GetValue<T>(string key, T? defaultValue = default)
    {
        if (_configuration.TryGetValue(key, out var value))
        {
            try
            {
                if (value is T directValue)
                    return directValue;

                if (value is JsonElement jsonElement)
                    return JsonSerializer.Deserialize<T>(jsonElement.GetRawText()) ?? defaultValue!;

                return (T?)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture) ?? defaultValue!;
            }
            catch (InvalidCastException)
            {
                return defaultValue!;
            }
            catch (FormatException)
            {
                return defaultValue!;
            }
            catch (OverflowException)
            {
                return defaultValue!;
            }
            catch (JsonException)
            {
                return defaultValue!;
            }
        }
        return defaultValue!;
    }

    public void SetValue<T>(string key, T value)
    {
        var oldValue = _configuration.TryGetValue(key, out var old) ? old : null;
        _configuration[key] = value!;

        ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(key, oldValue!, value!));
    }

    public string[] GetAvailableThemes()
    {
        return _availableThemes;
    }

    private void CreateDefaultConfiguration()
    {
        _configuration.Clear();
        _configuration["CurrentTheme"] = "Dark";
        _configuration["MaterialDesignBaseTheme"] = "Dark";
        _configuration["MaterialDesignPrimaryColor"] = "DeepPurple";
        _configuration["MaterialDesignSecondaryColor"] = "Lime";
        _configuration["UseSystemTheme"] = false;
        _configuration["WindowWidth"] = 1200.0;
        _configuration["WindowHeight"] = 800.0;
        _configuration["WindowMaximized"] = false;
        _configuration["RecentFiles"] = new List<string>();
    }

    private static object ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToArray(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            _ => element.GetRawText()
        };
    }
}
