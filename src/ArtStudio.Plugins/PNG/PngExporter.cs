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
/// PNG format exporter plugin
/// </summary>
[PluginMetadata(
    id: "png-exporter",
    name: "PNG Exporter",
    description: "Exports to PNG image files",
    author: "ArtStudio Team",
    version: "1.0.0"
)]
public class PngExporter : ExporterPluginBase
{
    public override string Id => "png-exporter";
    public override string Name => "PNG Exporter";
    public override string Description => "Exports to PNG image files";
    public override Version Version => new(1, 0, 0);
    public override string Author => "ArtStudio Team";

    public override string[] SupportedExtensions => new[] { ".png" };
    public override string[] SupportedMimeTypes => new[] { "image/png" };

    public override async Task<ExportResult> ExportAsync(ExportData data, string filePath, ExportOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var bitmap = new Bitmap(data.Width, data.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);

            // Set DPI if specified
            if (options?.Dpi.HasValue == true)
            {
                bitmap.SetResolution((float)options.Dpi.Value, (float)options.Dpi.Value);
            }
            else if (data.Dpi > 0)
            {
                bitmap.SetResolution((float)data.Dpi, (float)data.Dpi);
            }

            // Composite layers
            if (options?.FlattenLayers != false) // Default to flattening for PNG
            {
                await CompositeLayers(graphics, data.Layers, cancellationToken);
            }
            else if (data.Layers.Count > 0)
            {
                // For non-flattened PNG, just use the top visible layer
                var topLayer = data.Layers.FindLast(l => l.Visible);
                if (topLayer != null)
                {
                    await DrawLayer(graphics, topLayer, cancellationToken);
                }
            }

            // Save the image
            await SaveImageAsync(bitmap, filePath, options, cancellationToken);

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
                ErrorMessage = $"Failed to export PNG file: {ex.Message}"
            };
        }
    }

    private async Task CompositeLayers(Graphics graphics, List<ExportLayer> layers, CancellationToken cancellationToken)
    {
        foreach (var layer in layers)
        {
            if (!layer.Visible) continue;

            await DrawLayer(graphics, layer, cancellationToken);
        }
    }

    private async Task DrawLayer(Graphics graphics, ExportLayer layer, CancellationToken cancellationToken)
    {
        if (layer.ImageData.Length == 0) return;

        await Task.Yield(); // Make it properly async

        using var ms = new MemoryStream(layer.ImageData);
        using var layerImage = Image.FromStream(ms);

        // Apply opacity and blend mode (simplified)
        var imageAttributes = new ImageAttributes();
        if (layer.Opacity < 1.0f)
        {
            var colorMatrix = new ColorMatrix
            {
                Matrix33 = layer.Opacity
            };
            imageAttributes.SetColorMatrix(colorMatrix);
        }

        var destRect = new Rectangle(layer.X, layer.Y, layer.Width, layer.Height);
        graphics.DrawImage(layerImage, destRect, 0, 0, layerImage.Width, layerImage.Height, GraphicsUnit.Pixel, imageAttributes);
    }

    private async Task SaveImageAsync(Bitmap bitmap, string filePath, ExportOptions? options, CancellationToken cancellationToken)
    {
        await Task.Yield(); // Make it properly async

        var encoderParameters = new EncoderParameters(1);

        // PNG doesn't have quality settings like JPEG, but we can set compression level
        // For simplicity, we'll just save with default settings
        bitmap.Save(filePath, ImageFormat.Png);
    }
}
