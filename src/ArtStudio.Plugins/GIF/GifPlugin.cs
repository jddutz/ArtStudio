using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArtStudio.Core;
using ArtStudio.Core.Services;

namespace ArtStudio.Plugin.GIF;

[PluginMetadata(
    id: "gif-importer",
    name: "GIF Importer",
    description: "Imports GIF image files",
    author: "ArtStudio Team",
    version: "1.0.0"
)]
public class GifImporter : ImporterPluginBase
{
    public override string Id => "gif-importer";
    public override string Name => "GIF Importer";
    public override string Description => "Imports GIF image files";
    public override Version Version => new(1, 0, 0);
    public override string Author => "ArtStudio Team";

    public override string[] SupportedExtensions => new[] { ".gif" };
    public override string[] SupportedMimeTypes => new[] { "image/gif" };

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
                image.Save(ms, ImageFormat.Gif);
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
        catch (Exception ex)
        {
            return new ImportResult { Success = false, ErrorMessage = ex.Message };
        }
    }
}

[PluginMetadata(
    id: "gif-exporter",
    name: "GIF Exporter",
    description: "Exports to GIF image files",
    author: "ArtStudio Team",
    version: "1.0.0"
)]
public class GifExporter : ExporterPluginBase
{
    public override string Id => "gif-exporter";
    public override string Name => "GIF Exporter";
    public override string Description => "Exports to GIF image files";
    public override Version Version => new(1, 0, 0);
    public override string Author => "ArtStudio Team";

    public override string[] SupportedExtensions => new[] { ".gif" };
    public override string[] SupportedMimeTypes => new[] { "image/gif" };

    public override async Task<ExportResult> ExportAsync(ExportData data, string filePath, ExportOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Yield();
            using var bitmap = new Bitmap(data.Width, data.Height);
            using var graphics = Graphics.FromImage(bitmap);

            foreach (var layer in data.Layers.Where(l => l.Visible))
            {
                if (layer.ImageData.Length > 0)
                {
                    using var ms = new MemoryStream(layer.ImageData);
                    using var layerImage = Image.FromStream(ms);
                    graphics.DrawImage(layerImage, layer.X, layer.Y, layer.Width, layer.Height);
                }
            }

            bitmap.Save(filePath, ImageFormat.Gif);
            return new ExportResult { Success = true };
        }
        catch (Exception ex)
        {
            return new ExportResult { Success = false, ErrorMessage = ex.Message };
        }
    }
}
