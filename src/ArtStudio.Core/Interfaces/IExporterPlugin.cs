using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ArtStudio.Core;

/// <summary>
/// Interface for file export plugins
/// </summary>
public interface IExporterPlugin : IPlugin
{
    /// <summary>
    /// File extensions this exporter supports
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>
    /// MIME types this exporter supports
    /// </summary>
    IReadOnlyList<string> SupportedMimeTypes { get; }

    /// <summary>
    /// Check if the exporter can handle the specified format
    /// </summary>
    bool CanExport(string extension);

    /// <summary>
    /// Export data to a file
    /// </summary>
    Task<ExportResult> ExportAsync(ExportData data, string filePath, ExportOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export data to a stream
    /// </summary>
    Task<ExportResult> ExportAsync(ExportData data, Stream stream, string fileName, ExportOptions? options = null, CancellationToken cancellationToken = default);
}
