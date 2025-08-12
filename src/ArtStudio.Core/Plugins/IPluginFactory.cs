using System;

namespace ArtStudio.Core;

/// <summary>
/// Plugin factory interface for custom plugin creation
/// </summary>
public interface IPluginFactory<T> where T : IPlugin
{
    /// <summary>
    /// Create a plugin instance
    /// </summary>
    T CreatePlugin(Type pluginType, IPluginContext context);

    /// <summary>
    /// Check if this factory can create the specified type
    /// </summary>
    bool CanCreate(Type pluginType);
}
