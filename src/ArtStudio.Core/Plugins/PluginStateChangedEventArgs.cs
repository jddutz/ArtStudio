using System;

namespace ArtStudio.Core;

/// <summary>
/// Event arguments for plugin state changed event
/// </summary>
public class PluginStateChangedEventArgs : EventArgs
{
    public string PluginId { get; }
    public bool IsEnabled { get; }

    public PluginStateChangedEventArgs(string pluginId, bool isEnabled)
    {
        PluginId = pluginId;
        IsEnabled = isEnabled;
    }
}
