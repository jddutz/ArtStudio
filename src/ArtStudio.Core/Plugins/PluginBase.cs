using System;
using System.Collections.Generic;
using ArtStudio.Core;

namespace ArtStudio.Core.Services;

/// <summary>
/// Base implementation for plugins
/// </summary>
public abstract class PluginBase : IPlugin
{
    private bool _isEnabled = true;
    private IPluginContext? _context;

    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract Version Version { get; }
    public abstract string Author { get; }

    public virtual bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    protected IPluginContext? Context => _context;

    public virtual void Initialize(IPluginContext context)
    {
        _context = context;
        OnInitialize(context);
    }

    public virtual void Dispose()
    {
        OnDispose();
        _context = null;
    }

    protected virtual void OnInitialize(IPluginContext context) { }
    protected virtual void OnDispose() { }
}

/// <summary>
/// Base implementation for tool settings
/// </summary>
public class ToolSettingsBase : IToolSettings
{
    private readonly Dictionary<string, object> _settings = new();

    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    public virtual Dictionary<string, object> GetSettings()
    {
        return new Dictionary<string, object>(_settings);
    }

    public virtual void SetSetting(string key, object value)
    {
        var oldValue = _settings.TryGetValue(key, out var existing) ? existing : null;
        _settings[key] = value;
        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(key, oldValue, value));
    }

    public virtual T? GetSetting<T>(string key, T? defaultValue = default)
    {
        if (_settings.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    public virtual void ResetToDefaults()
    {
        var oldSettings = new Dictionary<string, object>(_settings);
        _settings.Clear();
        OnResetToDefaults();

        foreach (var (key, oldValue) in oldSettings)
        {
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(key, oldValue, null));
        }
    }

    protected virtual void OnResetToDefaults() { }
}

/// <summary>
/// Base implementation for filter settings
/// </summary>
public class FilterSettingsBase : IFilterSettings
{
    private readonly Dictionary<string, object> _settings = new();

    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    public virtual Dictionary<string, object> GetSettings()
    {
        return new Dictionary<string, object>(_settings);
    }

    public virtual void SetSetting(string key, object value)
    {
        var oldValue = _settings.TryGetValue(key, out var existing) ? existing : null;
        _settings[key] = value;
        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(key, oldValue, value));
    }

    public virtual T? GetSetting<T>(string key, T? defaultValue = default)
    {
        if (_settings.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    public virtual void ResetToDefaults()
    {
        var oldSettings = new Dictionary<string, object>(_settings);
        _settings.Clear();
        OnResetToDefaults();

        foreach (var (key, oldValue) in oldSettings)
        {
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(key, oldValue, null));
        }
    }

    public virtual IFilterSettings Clone()
    {
        var clone = CreateClone();
        foreach (var (key, value) in _settings)
        {
            clone.SetSetting(key, value);
        }
        return clone;
    }

    protected virtual FilterSettingsBase CreateClone()
    {
        return new FilterSettingsBase();
    }

    protected virtual void OnResetToDefaults() { }
}

/// <summary>
/// Base implementation for generator settings
/// </summary>
public class GeneratorSettingsBase : IGeneratorSettings
{
    private readonly Dictionary<string, object> _settings = new();

    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    public virtual Dictionary<string, object> GetSettings()
    {
        return new Dictionary<string, object>(_settings);
    }

    public virtual void SetSetting(string key, object value)
    {
        var oldValue = _settings.TryGetValue(key, out var existing) ? existing : null;
        _settings[key] = value;
        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(key, oldValue, value));
    }

    public virtual T? GetSetting<T>(string key, T? defaultValue = default)
    {
        if (_settings.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    public virtual void ResetToDefaults()
    {
        var oldSettings = new Dictionary<string, object>(_settings);
        _settings.Clear();
        OnResetToDefaults();

        foreach (var (key, oldValue) in oldSettings)
        {
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(key, oldValue, null));
        }
    }

    public virtual IGeneratorSettings Clone()
    {
        var clone = CreateClone();
        foreach (var (key, value) in _settings)
        {
            clone.SetSetting(key, value);
        }
        return clone;
    }

    protected virtual GeneratorSettingsBase CreateClone()
    {
        return new GeneratorSettingsBase();
    }

    protected virtual void OnResetToDefaults() { }
}
