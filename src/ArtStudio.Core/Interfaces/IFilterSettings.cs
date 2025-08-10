using System;
using System.Collections.Generic;

namespace ArtStudio.Core;

/// <summary>
/// Interface for filter settings
/// </summary>
public interface IFilterSettings
{
    /// <summary>
    /// Get all settings as key-value pairs
    /// </summary>
    Dictionary<string, object> GetSettings();

    /// <summary>
    /// Set a setting value
    /// </summary>
    void SetSetting(string key, object value);

    /// <summary>
    /// Get a setting value
    /// </summary>
    T? GetSetting<T>(string key, T? defaultValue = default);

    /// <summary>
    /// Reset all settings to defaults
    /// </summary>
    void ResetToDefaults();

    /// <summary>
    /// Clone the settings
    /// </summary>
    IFilterSettings Clone();

    /// <summary>
    /// Event raised when settings change
    /// </summary>
    event EventHandler<SettingsChangedEventArgs> SettingsChanged;
}
