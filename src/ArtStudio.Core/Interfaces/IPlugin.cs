using System;
using System.Collections.Generic;

namespace ArtStudio.Core.Interfaces;

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

/// <summary>
/// Context provided to plugins during initialization
/// </summary>
public interface IPluginContext
{
    /// <summary>
    /// Service provider for dependency injection
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Configuration manager for plugin settings
    /// </summary>
    IConfigurationManager ConfigurationManager { get; }

    /// <summary>
    /// Plugin-specific configuration data
    /// </summary>
    Dictionary<string, object> PluginData { get; }
}

/// <summary>
/// Metadata attribute for plugin discovery
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class PluginMetadataAttribute : Attribute
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string Author { get; }
    public string Version { get; }
    public Type[]? Dependencies { get; set; }
    public string[]? SupportedFormats { get; set; }

    public PluginMetadataAttribute(string id, string name, string description, string author, string version)
    {
        Id = id;
        Name = name;
        Description = description;
        Author = author;
        Version = version;
    }
}
