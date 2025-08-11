using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArtStudio.Core;

namespace ArtStudio.Core.Services;

/// <summary>
/// Base implementation for exporter plugins
/// </summary>
public abstract class ExporterPluginBase : PluginBase, IExporterPlugin
{
    public abstract IReadOnlyList<string> SupportedExtensions { get; }
    public abstract IReadOnlyList<string> SupportedMimeTypes { get; }

    public virtual bool CanExport(string extension)
    {
        ArgumentNullException.ThrowIfNull(extension);
        var normalizedExtension = extension.StartsWith('.') ? extension.ToUpperInvariant() : $".{extension.ToUpperInvariant()}";
        return SupportedExtensions.Any(ext => ext.Equals(normalizedExtension, StringComparison.OrdinalIgnoreCase));
    }

    public abstract Task<ExportResult> ExportAsync(ExportData data, string filePath, ExportOptions? options = null, CancellationToken cancellationToken = default);

    public virtual async Task<ExportResult> ExportAsync(ExportData data, Stream stream, string fileName, ExportOptions? options = null, CancellationToken cancellationToken = default)
    {
        // Default implementation: export to temp file and copy to stream
        var tempPath = Path.GetTempFileName();
        var extension = Path.GetExtension(fileName);
        var tempFileWithExtension = Path.ChangeExtension(tempPath, extension);

        try
        {
            var result = await ExportAsync(data, tempFileWithExtension, options, cancellationToken).ConfigureAwait(false);

            if (result.Success && File.Exists(tempFileWithExtension))
            {
                using (var fileStream = File.OpenRead(tempFileWithExtension))
                {
                    await fileStream.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
                }
            }

            return result;
        }
        finally
        {
            if (File.Exists(tempFileWithExtension))
            {
                File.Delete(tempFileWithExtension);
            }
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
