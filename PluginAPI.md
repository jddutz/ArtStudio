# ArtStudio Plugin Component Model

ArtStudio now features a comprehensive plugin component model that allows for extensible functionality through various types of plugins.

## Overview

The plugin system supports the following component types:

- **Importers** - Import data from various file formats
- **Exporters** - Export data to various file formats
- **Tools** - Interactive tools for the canvas
- **Layer Filters** - Apply effects and transformations to layers
- **Image Generators** - Generate images procedurally or using AI

## Architecture

### Core Components

- `IPlugin` - Base interface for all plugins
- `IPluginManager` - Manages plugin loading, enabling/disabling, and lifecycle
- `IPluginContext` - Provides access to services and configuration for plugins
- `PluginMetadata` - Attributes for describing plugin capabilities

### Plugin Types

#### Importer Plugins (`IImporterPlugin`)

Import data from external files into the application.

```csharp
[PluginMetadata(
    id: "my-importer",
    name: "My File Importer",
    description: "Imports custom file format",
    author: "Your Name",
    version: "1.0.0",
    supportedFormats: new[] { ".myext" }
)]
public class MyImporter : ImporterPluginBase
{
    public override async Task<ImportResult> ImportAsync(string filePath, ImportOptions options)
    {
        // Implementation
    }

    public override bool CanImport(string filePath)
    {
        // Implementation
    }
}
```

#### Exporter Plugins (`IExporterPlugin`)

Export data from the application to external files.

```csharp
[PluginMetadata(
    id: "my-exporter",
    name: "My File Exporter",
    description: "Exports to custom file format",
    author: "Your Name",
    version: "1.0.0",
    supportedFormats: new[] { ".myext" }
)]
public class MyExporter : ExporterPluginBase
{
    public override async Task<ExportResult> ExportAsync(object[] items, string outputPath, ExportOptions options)
    {
        // Implementation
    }

    public override bool CanExport(object[] items)
    {
        // Implementation
    }
}
```

#### Tool Plugins (`IToolPlugin`)

Interactive tools for the canvas.

```csharp
[PluginMetadata(
    id: "my-tool",
    name: "My Custom Tool",
    description: "A custom drawing tool",
    author: "Your Name",
    version: "1.0.0"
)]
public class MyTool : ToolPluginBase
{
    public override string Name => "My Tool";
    public override string IconPath => "/icons/my-tool.png";
    public override string Category => "Custom";

    public override void OnToolSelected()
    {
        // Tool activation logic
    }

    public override bool OnCanvasClick(double x, double y, object? additionalData = null)
    {
        // Handle canvas interaction
        return true; // Return true if handled
    }
}
```

#### Layer Filter Plugins (`ILayerFilterPlugin`)

Apply effects and transformations to layers.

```csharp
[PluginMetadata(
    id: "my-filter",
    name: "My Custom Filter",
    description: "A custom layer filter",
    author: "Your Name",
    version: "1.0.0"
)]
public class MyFilter : LayerFilterPluginBase
{
    public override string Name => "My Filter";
    public override string Category => "Effects";

    public override async Task<FilterResult> ApplyFilterAsync(object layerData, FilterOptions options)
    {
        // Apply filter logic
    }

    public override bool CanApplyTo(object layerData)
    {
        // Check if filter can be applied
    }
}
```

#### Image Generator Plugins (`IImageGeneratorPlugin`)

Generate images procedurally or using AI.

```csharp
[PluginMetadata(
    id: "my-generator",
    name: "My Image Generator",
    description: "Generates custom images",
    author: "Your Name",
    version: "1.0.0"
)]
public class MyGenerator : ImageGeneratorPluginBase
{
    public override string Name => "My Generator";
    public override GeneratorType Type => GeneratorType.Procedural;

    public override async Task<GenerationResult> GenerateAsync(GenerationRequest request)
    {
        // Image generation logic
    }

    public override GenerationCapabilities GetCapabilities()
    {
        // Return supported capabilities
    }
}
```

## Plugin Loading

Plugins are automatically loaded from the following directories:

- `{AppDirectory}/Plugins`
- `{AppDirectory}/Extensions`
- `%AppData%/ArtStudio/Plugins`
- `%LocalAppData%/ArtStudio/Plugins`

## Dependency Injection

The plugin system is fully integrated with the application's dependency injection container. Plugins can:

- Access registered services through `IPluginContext.ServiceProvider`
- Request configuration through `IPluginContext.ConfigurationManager`
- Log messages and errors through the logging system

## Example Plugin Project

See the `ExamplePlugins/ArtStudio.SamplePlugin` project for working examples of each plugin type.

## Building and Deploying Plugins

1. Create a new .NET 8 class library project
2. Reference `ArtStudio.Core`
3. Implement your plugin classes with the appropriate base classes and metadata attributes
4. Build the project
5. Copy the output assembly to one of the plugin directories

## Plugin Validation

The plugin manager validates plugins for:

- Proper metadata attributes
- Interface implementation
- Dependency resolution
- Security and stability

## Future Extensions

The plugin system is designed to be extensible. New plugin types can be easily added by:

1. Creating new interfaces inheriting from `IPlugin`
2. Adding base implementation classes
3. Registering the new plugin type in the plugin manager
4. Creating appropriate UI integration points

This allows the plugin ecosystem to grow organically based on user and developer needs.
