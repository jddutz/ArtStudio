using System;

namespace ArtStudio.Core;

/// <summary>
/// Event arguments for configuration changes
/// </summary>
public class ConfigurationChangedEventArgs : EventArgs
{
    public string Key { get; }
    public object OldValue { get; }
    public object NewValue { get; }

    public ConfigurationChangedEventArgs(string key, object oldValue, object newValue)
    {
        Key = key;
        OldValue = oldValue;
        NewValue = newValue;
    }
}
