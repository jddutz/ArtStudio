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
/// OpenRaster format exporter plugin
/// </summary>
[PluginMetadata(
    id: "openraster-exporter",
    name: "OpenRaster Exporter",
    description: "Exports to OpenRaster (.ora) files",
    author: "ArtStudio Team",
    version: "1.0.0"
)]
public class OpenRasterExporter : ExporterPluginBase
{
    public override string Id => "openraster-exporter";
    public override string Name => "OpenRaster Exporter";
    public override string Description => "Exports to OpenRaster (.ora) files";
    public override Version Version => new(1, 0, 0);
    public override string Author => "ArtStudio Team";

    public override string[] SupportedExtensions => new[] { ".ora" };
    public override string[] SupportedMimeTypes => new[] { "image/openraster" };

    public override async Task<ExportResult> ExportAsync(ExportData data, string filePath, ExportOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var fileStream = File.Create(filePath);
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);

            // Create mimetype entry (must be first and uncompressed)
            var mimetypeEntry = archive.CreateEntry("mimetype", CompressionLevel.NoCompression);
            using (var mimetypeStream = mimetypeEntry.Open())
            using (var writer = new StreamWriter(mimetypeStream))
            {
                await writer.WriteAsync("image/openraster");
            }

            // Create stack.xml
            var stackXml = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("image",
                    new XAttribute("w", data.Width),
                    new XAttribute("h", data.Height),
                    new XElement("stack",
                        from layer in data.Layers
                        select new XElement("layer",
                            new XAttribute("name", layer.Name),
                            new XAttribute("x", layer.X),
                            new XAttribute("y", layer.Y),
                            new XAttribute("opacity", layer.Opacity),
                            new XAttribute("visibility", layer.Visible ? "visible" : "hidden"),
                            new XAttribute("composite-op", layer.BlendMode),
                            new XAttribute("src", $"data/{layer.Name}.png")
                        )
                    )
                )
            );

            var stackEntry = archive.CreateEntry("stack.xml");
            using (var stackStream = stackEntry.Open())
            {
                await stackXml.SaveAsync(stackStream, SaveOptions.None, cancellationToken);
            }

            // Create data directory and save layer images
            for (int i = 0; i < data.Layers.Count; i++)
            {
                var layer = data.Layers[i];
                var layerEntry = archive.CreateEntry($"data/{layer.Name}.png");
                using var layerStream = layerEntry.Open();
                await layerStream.WriteAsync(layer.ImageData, cancellationToken);
            }

            return new ExportResult
            {
                Success = true,
                Metadata = new ExportMetadata
                {
                    FileSize = new FileInfo(filePath).Length
                }
            };
        }
        catch (Exception ex)
        {
            return new ExportResult
            {
                Success = false,
                ErrorMessage = $"Failed to export OpenRaster file: {ex.Message}"
            };
        }
    }
}
