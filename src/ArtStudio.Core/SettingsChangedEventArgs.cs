using System;

namespace ArtStudio.Core;

/// <summary>
/// Settings changed event arguments
/// </summary>
public class SettingsChangedEventArgs : EventArgs
{
    public string SettingKey { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }

    public SettingsChangedEventArgs(string settingKey, object? oldValue, object? newValue)
    {
        SettingKey = settingKey;
        OldValue = oldValue;
        NewValue = newValue;
    }
}
