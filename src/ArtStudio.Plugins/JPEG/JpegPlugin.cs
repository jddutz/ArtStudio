using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArtStudio.Core;
using ArtStudio.Core.Services;

namespace ArtStudio.Plugin.JPEG;

/// <summary>
/// JPEG format importer/exporter plugin
/// </summary>
[PluginMetadata(
    id: "jpeg-importer",
    name: "JPEG Importer",
    description: "Imports JPEG image files",
    author: "ArtStudio Team",
    version: "1.0.0"
)]
public class JpegImporter : ImporterPluginBase
{
    public override string Id => "jpeg-importer";
    public override string Name => "JPEG Importer";
    public override string Description => "Imports JPEG image files";
    public override Version Version => new(1, 0, 0);
    public override string Author => "ArtStudio Team";

    public override string[] SupportedExtensions => new[] { ".jpg", ".jpeg" };
    public override string[] SupportedMimeTypes => new[] { "image/jpeg" };

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

            byte[] imageData;
            using (var ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Jpeg);
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

            return new ImportResult
            {
                Success = true,
                Document = document,
                Metadata = new ImportMetadata { Properties = { { "Format", "JPEG" } } }
            };
        }
        catch (Exception ex)
        {
            return new ImportResult
            {
                Success = false,
                ErrorMessage = $"Failed to import JPEG: {ex.Message}"
            };
        }
    }
}

[PluginMetadata(
    id: "jpeg-exporter",
    name: "JPEG Exporter",
    description: "Exports to JPEG image files",
    author: "ArtStudio Team",
    version: "1.0.0"
)]
public class JpegExporter : ExporterPluginBase
{
    public override string Id => "jpeg-exporter";
    public override string Name => "JPEG Exporter";
    public override string Description => "Exports to JPEG image files";
    public override Version Version => new(1, 0, 0);
    public override string Author => "ArtStudio Team";

    public override string[] SupportedExtensions => new[] { ".jpg", ".jpeg" };
    public override string[] SupportedMimeTypes => new[] { "image/jpeg" };

    public override async Task<ExportResult> ExportAsync(ExportData data, string filePath, ExportOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Yield();
            using var bitmap = new Bitmap(data.Width, data.Height);
            using var graphics = Graphics.FromImage(bitmap);

            // Composite layers (JPEG doesn't support transparency, so always flatten)
            foreach (var layer in data.Layers.Where(l => l.Visible))
            {
                if (layer.ImageData.Length > 0)
                {
                    using var ms = new MemoryStream(layer.ImageData);
                    using var layerImage = Image.FromStream(ms);
                    graphics.DrawImage(layerImage, layer.X, layer.Y, layer.Width, layer.Height);
                }
            }

            // Save with quality setting
            var encoder = ImageCodecInfo.GetImageDecoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, options?.Quality ?? 85L);

            bitmap.Save(filePath, encoder, encoderParams);

            return new ExportResult
            {
                Success = true,
                Metadata = new ExportMetadata { FileSize = new FileInfo(filePath).Length }
            };
        }
        catch (Exception ex)
        {
            return new ExportResult
            {
                Success = false,
                ErrorMessage = $"Failed to export JPEG: {ex.Message}"
            };
        }
    }
}
