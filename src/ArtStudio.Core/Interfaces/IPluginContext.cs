using System;
using System.Collections.Generic;

namespace ArtStudio.Core;

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
