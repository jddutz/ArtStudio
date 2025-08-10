using System;
using System.Collections.Generic;

namespace ArtStudio.Core;

/// <summary>
/// Event arguments for plugins loaded event
/// </summary>
public class PluginsLoadedEventArgs : EventArgs
{
    public List<PluginMetadata> LoadedPlugins { get; }
    public List<PluginLoadError> LoadErrors { get; }

    public PluginsLoadedEventArgs(List<PluginMetadata> loadedPlugins, List<PluginLoadError> loadErrors)
    {
        LoadedPlugins = loadedPlugins;
        LoadErrors = loadErrors;
    }
}
