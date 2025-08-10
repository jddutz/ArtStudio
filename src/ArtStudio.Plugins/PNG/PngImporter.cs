using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ArtStudio.Core.Interfaces;
using ArtStudio.Core.Services;

namespace ArtStudio.Plugin.PNG;

/// <summary>
/// PNG format importer plugin
/// </summary>
[PluginMetadata(
    id: "png-importer",
    name: "PNG Importer",
    description: "Imports PNG image files",
    author: "ArtStudio Team",
    version: "1.0.0"
)]
public class PngImporter : ImporterPluginBase
{
    public override string Id => "png-importer";
    public override string Name => "PNG Importer";
    public override string Description => "Imports PNG image files";
    public override Version Version => new(1, 0, 0);
    public override string Author => "ArtStudio Team";

    public override string[] SupportedExtensions => new[] { ".png" };
    public override string[] SupportedMimeTypes => new[] { "image/png" };

    public override async Task<ImportResult> ImportAsync(string filePath, ImportOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var image = await LoadImageAsync(filePath, cancellationToken);

            var document = new ImportedDocument
            {
                Width = image.Width,
                Height = image.Height,
                Dpi = image.HorizontalResolution
            };

            // Convert image to byte array
            byte[] imageData;
            using (var ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Png);
                imageData = ms.ToArray();
            }

            var layer = new ImportedLayer
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
            };

            document.Layers.Add(layer);

            return new ImportResult
            {
                Success = true,
                Document = document,
                Metadata = new ImportMetadata
                {
                    Properties = { { "Format", "PNG" } },
                    CreatedDate = File.GetCreationTime(filePath),
                    ModifiedDate = File.GetLastWriteTime(filePath)
                }
            };
        }
        catch (Exception ex)
        {
            return new ImportResult
            {
                Success = false,
                ErrorMessage = $"Failed to import PNG file: {ex.Message}"
            };
        }
    }

    private async Task<Bitmap> LoadImageAsync(string filePath, CancellationToken cancellationToken)
    {
        // For simplicity, using synchronous image loading
        // In a real implementation, you might want to use async file I/O
        await Task.Yield(); // Yield to make it properly async
        return new Bitmap(filePath);
    }
}
