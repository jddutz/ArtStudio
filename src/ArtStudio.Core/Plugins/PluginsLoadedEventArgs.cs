using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ArtStudio.Core;

/// <summary>
/// Event arguments for plugins loaded event
/// </summary>
public class PluginsLoadedEventArgs : EventArgs
{
    public Collection<PluginMetadata> LoadedPlugins { get; }
    public Collection<PluginLoadError> LoadErrors { get; }

    public PluginsLoadedEventArgs(Collection<PluginMetadata> loadedPlugins, Collection<PluginLoadError> loadErrors)
    {
        LoadedPlugins = loadedPlugins;
        LoadErrors = loadErrors;
    }
}
