using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ArtStudio.Core.Interfaces;

namespace ArtStudio.Core.Services;

/// <summary>
/// Base implementation for importer plugins
/// </summary>
public abstract class ImporterPluginBase : PluginBase, IImporterPlugin
{
    public abstract string[] SupportedExtensions { get; }
    public abstract string[] SupportedMimeTypes { get; }

    public virtual bool CanImport(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return Array.Exists(SupportedExtensions, ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
    }

    public abstract Task<ImportResult> ImportAsync(string filePath, ImportOptions? options = null, CancellationToken cancellationToken = default);

    public virtual async Task<ImportResult> ImportAsync(Stream stream, string fileName, ImportOptions? options = null, CancellationToken cancellationToken = default)
    {
        // Default implementation: save to temp file and import from file
        var tempPath = Path.GetTempFileName();
        var extension = Path.GetExtension(fileName);
        var tempFileWithExtension = Path.ChangeExtension(tempPath, extension);

        try
        {
            using (var fileStream = File.Create(tempFileWithExtension))
            {
                await stream.CopyToAsync(fileStream, cancellationToken);
            }

            return await ImportAsync(tempFileWithExtension, options, cancellationToken);
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
