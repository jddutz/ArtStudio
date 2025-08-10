using ArtStudio.Core.Interfaces;
using ArtStudio.Core.Services;

namespace ArtStudio.SamplePlugin;

/// <summary>
/// Sample importer plugin that demonstrates the plugin architecture
/// </summary>
[PluginMetadata(
    id: "sample-text-importer",
    name: "Sample Text Importer",
    description: "A sample plugin that imports text files as simple text layers",
    author: "ArtStudio Team",
    version: "1.0.0",
    supportedFormats: new[] { ".txt", ".md" }
)]
public class SampleTextImporter : ImporterPluginBase
{
    public override async Task<ImportResult> ImportAsync(string filePath, ImportOptions options)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filePath);

            var result = new ImportResult
            {
                Success = true,
                Message = $"Successfully imported {Path.GetFileName(filePath)}"
            };

            // Create a simple text layer
            result.ImportedItems.Add(new ImportedItem
            {
                Name = Path.GetFileNameWithoutExtension(filePath),
                Type = "TextLayer",
                Data = content,
                Properties = new Dictionary<string, object>
                {
                    { "FontSize", 12 },
                    { "FontFamily", "Arial" },
                    { "Color", "#000000" }
                }
            });

            return result;
        }
        catch (Exception ex)
        {
            return new ImportResult
            {
                Success = false,
                Message = $"Failed to import {filePath}: {ex.Message}"
            };
        }
    }

    public override bool CanImport(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension == ".txt" || extension == ".md";
    }
}

/// <summary>
/// Sample exporter plugin that demonstrates the plugin architecture
/// </summary>
[PluginMetadata(
    id: "sample-text-exporter",
    name: "Sample Text Exporter",
    description: "A sample plugin that exports text layers to text files",
    author: "ArtStudio Team",
    version: "1.0.0",
    supportedFormats: new[] { ".txt" }
)]
public class SampleTextExporter : ExporterPluginBase
{
    public override async Task<ExportResult> ExportAsync(object[] items, string outputPath, ExportOptions options)
    {
        try
        {
            var textItems = items.OfType<Dictionary<string, object>>()
                .Where(item => item.ContainsKey("Type") && item["Type"].ToString() == "TextLayer")
                .ToList();

            if (!textItems.Any())
            {
                return new ExportResult
                {
                    Success = false,
                    Message = "No text layers found to export"
                };
            }

            var allText = string.Join("\n\n", textItems.Select(item =>
                item.ContainsKey("Data") ? item["Data"].ToString() : ""));

            await File.WriteAllTextAsync(outputPath, allText);

            return new ExportResult
            {
                Success = true,
                Message = $"Successfully exported {textItems.Count} text layer(s) to {Path.GetFileName(outputPath)}"
            };
        }
        catch (Exception ex)
        {
            return new ExportResult
            {
                Success = false,
                Message = $"Failed to export to {outputPath}: {ex.Message}"
            };
        }
    }

    public override bool CanExport(object[] items)
    {
        return items.OfType<Dictionary<string, object>>()
            .Any(item => item.ContainsKey("Type") && item["Type"].ToString() == "TextLayer");
    }
}

/// <summary>
/// Sample tool plugin that demonstrates the plugin architecture
/// </summary>
[PluginMetadata(
    id: "sample-text-tool",
    name: "Sample Text Tool",
    description: "A sample tool that adds text to the canvas",
    author: "ArtStudio Team",
    version: "1.0.0"
)]
public class SampleTextTool : ToolPluginBase
{
    public override string Name => "Text Tool";
    public override string IconPath => "/icons/text-tool.png";
    public override string Category => "Text";

    public override void OnToolSelected()
    {
        // Tool selection logic
        Context?.LogMessage("Text tool selected");
    }

    public override void OnToolDeselected()
    {
        // Tool deselection logic
        Context?.LogMessage("Text tool deselected");
    }

    public override bool OnCanvasClick(double x, double y, object? additionalData = null)
    {
        // Handle canvas click - for example, add a text element at the clicked position
        Context?.LogMessage($"Text tool clicked at ({x}, {y})");

        // Return true to indicate the click was handled
        return true;
    }

    public override bool OnCanvasDrag(double startX, double startY, double endX, double endY, object? additionalData = null)
    {
        // Handle canvas drag - for example, create a text box with drag area
        Context?.LogMessage($"Text tool dragged from ({startX}, {startY}) to ({endX}, {endY})");

        // Return true to indicate the drag was handled
        return true;
    }
}
