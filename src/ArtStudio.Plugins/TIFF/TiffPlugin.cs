using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArtStudio.Core;
using ArtStudio.Core.Services;

namespace ArtStudio.Plugin.TIFF;

[PluginMetadata(
    id: "tiff-importer",
    name: "TIFF Importer",
    description: "Imports TIFF image files",
    author: "ArtStudio Team",
    version: "1.0.0"
)]
public class TiffImporter : ImporterPluginBase
{
    public override string Id => "tiff-importer";
    public override string Name => "TIFF Importer";
    public override string Description => "Imports TIFF image files";
    public override Version Version => new(1, 0, 0);
    public override string Author => "ArtStudio Team";

    public override string[] SupportedExtensions => new[] { ".tiff", ".tif" };
    public override string[] SupportedMimeTypes => new[] { "image/tiff" };

    public override async Task<ImportResult> ImportAsync(string filePath, ImportOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Yield();
            using var image = new Bitmap(filePath);

            var document = new ImportedDocument
            {
                Width = image.Width,
                Height = image.Height,
                Dpi = image.HorizontalResolution
            };

            // TIFF can contain multiple frames/layers - for now, just handle the first frame
            byte[] imageData;
            using (var ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Tiff);
                imageData = ms.ToArray();
            }

            document.Layers.Add(new ImportedLayer
            {
                Name = Path.GetFileNameWithoutExtension(filePath),
                ImageData = imageData,
                X = 0,
                Y = 0,
                Width = image.Width,
                Height = image.Height,
                Opacity = 1.0f,
                Visible = true,
                BlendMode = "Normal"
            });

            return new ImportResult { Success = true, Document = document };
        }
#pragma warning disable CA1031 // Do not catch general exception types
        // Gracefully handle plugin errors by returning failure result instead of crashing
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            return new ImportResult { Success = false, ErrorMessage = ex.Message };
        }
    }
}

[PluginMetadata(
    id: "tiff-exporter",
    name: "TIFF Exporter",
    description: "Exports to TIFF image files",
    author: "ArtStudio Team",
    version: "1.0.0"
)]
public class TiffExporter : ExporterPluginBase
{
    public override string Id => "tiff-exporter";
    public override string Name => "TIFF Exporter";
    public override string Description => "Exports to TIFF image files";
    public override Version Version => new(1, 0, 0);
    public override string Author => "ArtStudio Team";

    public override string[] SupportedExtensions => new[] { ".tiff", ".tif" };
    public override string[] SupportedMimeTypes => new[] { "image/tiff" };

    public override async Task<ExportResult> ExportAsync(ExportData data, string filePath, ExportOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        try
        {
            await Task.Yield();
            using var bitmap = new Bitmap(data.Width, data.Height);
            using var graphics = Graphics.FromImage(bitmap);

            // Set DPI for TIFF
            if (options?.Dpi.HasValue == true)
            {
                bitmap.SetResolution((float)options.Dpi.Value, (float)options.Dpi.Value);
            }
            else if (data.Dpi > 0)
            {
                bitmap.SetResolution((float)data.Dpi, (float)data.Dpi);
            }

            // Composite layers - TIFF can preserve layers but for simplicity we'll flatten
            foreach (var layer in data.Layers.Where(l => l.Visible))
            {
                if (layer.ImageData.Count > 0)
                {
                    using var ms = new MemoryStream(layer.ImageData.ToArray());
                    using var layerImage = Image.FromStream(ms);
                    graphics.DrawImage(layerImage, layer.X, layer.Y, layer.Width, layer.Height);
                }
            }

            bitmap.Save(filePath, ImageFormat.Tiff);
            return new ExportResult { Success = true };
        }
#pragma warning disable CA1031 // Do not catch general exception types
        // Gracefully handle plugin errors by returning failure result instead of crashing
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            return new ExportResult { Success = false, ErrorMessage = ex.Message };
        }
    }
}
