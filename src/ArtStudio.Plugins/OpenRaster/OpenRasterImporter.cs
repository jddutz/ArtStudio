using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using ArtStudio.Core.Interfaces;
using ArtStudio.Core.Services;

namespace ArtStudio.Plugin.OpenRaster;

/// <summary>
/// OpenRaster format importer plugin
/// </summary>
[PluginMetadata(
    id: "openraster-importer",
    name: "OpenRaster Importer",
    description: "Imports OpenRaster (.ora) files",
    author: "ArtStudio Team",
    version: "1.0.0"
)]
public class OpenRasterImporter : ImporterPluginBase
{
    public override string Id => "openraster-importer";
    public override string Name => "OpenRaster Importer";
    public override string Description => "Imports OpenRaster (.ora) files";
    public override Version Version => new(1, 0, 0);
    public override string Author => "ArtStudio Team";

    public override string[] SupportedExtensions => new[] { ".ora" };
    public override string[] SupportedMimeTypes => new[] { "image/openraster" };

    public override async Task<ImportResult> ImportAsync(string filePath, ImportOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var archive = ZipFile.OpenRead(filePath);

            // Read stack.xml to get layer information
            var stackEntry = archive.GetEntry("stack.xml");
            if (stackEntry == null)
            {
                return new ImportResult
                {
                    Success = false,
                    ErrorMessage = "Invalid OpenRaster file: missing stack.xml"
                };
            }

            XDocument stackXml;
            using (var stream = stackEntry.Open())
            {
                stackXml = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
            }

            var document = new ImportedDocument();

            // Parse image dimensions from stack.xml
            var imageElement = stackXml.Root?.Element("image");
            if (imageElement != null)
            {
                document.Width = int.Parse(imageElement.Attribute("w")?.Value ?? "0");
                document.Height = int.Parse(imageElement.Attribute("h")?.Value ?? "0");
            }

            // Parse layers
            var stackElement = imageElement?.Element("stack");
            if (stackElement != null)
            {
                await ParseLayers(stackElement, document.Layers, archive, cancellationToken);
            }

            return new ImportResult
            {
                Success = true,
                Document = document,
                Metadata = new ImportMetadata
                {
                    Properties = { { "Format", "OpenRaster" } }
                }
            };
        }
        catch (Exception ex)
        {
            return new ImportResult
            {
                Success = false,
                ErrorMessage = $"Failed to import OpenRaster file: {ex.Message}"
            };
        }
    }

    private async Task ParseLayers(XElement stackElement, List<ImportedLayer> layers, ZipArchive archive, CancellationToken cancellationToken)
    {
        foreach (var layerElement in stackElement.Elements("layer"))
        {
            var layer = new ImportedLayer
            {
                Name = layerElement.Attribute("name")?.Value ?? "Unnamed Layer",
                X = int.Parse(layerElement.Attribute("x")?.Value ?? "0"),
                Y = int.Parse(layerElement.Attribute("y")?.Value ?? "0"),
                Opacity = float.Parse(layerElement.Attribute("opacity")?.Value ?? "1.0"),
                Visible = layerElement.Attribute("visibility")?.Value != "hidden",
                BlendMode = layerElement.Attribute("composite-op")?.Value ?? "svg:src-over"
            };

            // Load layer image data
            var srcAttribute = layerElement.Attribute("src")?.Value;
            if (!string.IsNullOrEmpty(srcAttribute))
            {
                var entry = archive.GetEntry(srcAttribute);
                if (entry != null)
                {
                    using var stream = entry.Open();
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream, cancellationToken);
                    layer.ImageData = memoryStream.ToArray();

                    // For simplicity, we'll assume the PNG dimensions match the layer
                    // In a real implementation, you'd decode the PNG to get actual dimensions
                    layer.Width = 100; // Placeholder
                    layer.Height = 100; // Placeholder
                }
            }

            layers.Add(layer);
        }
    }
}
