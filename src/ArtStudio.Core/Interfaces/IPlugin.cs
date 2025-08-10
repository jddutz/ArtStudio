using System;

namespace ArtStudio.Core;

/// <summary>
/// Base interface for all plugin components
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Unique identifier for the plugin
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Display name for the plugin
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Plugin description
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Plugin version
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Plugin author/developer
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Indicates if the plugin is currently enabled
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Initialize the plugin with the given context
    /// </summary>
    /// <param name="context">Plugin initialization context</param>
    void Initialize(IPluginContext context);

    /// <summary>
    /// Cleanup resources when plugin is unloaded
    /// </summary>
    void Dispose();
}
