using System;

namespace ArtStudio.Core;

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
