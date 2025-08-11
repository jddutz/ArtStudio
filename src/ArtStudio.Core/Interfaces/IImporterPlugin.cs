using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ArtStudio.Core;

/// <summary>
/// Interface for file import plugins
/// </summary>
public interface IImporterPlugin : IPlugin
{
    /// <summary>
    /// File extensions this importer supports (e.g., ".jpg", ".png")
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>
    /// MIME types this importer supports
    /// </summary>
    IReadOnlyList<string> SupportedMimeTypes { get; }

    /// <summary>
    /// Check if the importer can handle the specified file
    /// </summary>
    bool CanImport(string filePath);

    /// <summary>
    /// Import a file and return the imported data
    /// </summary>
    Task<ImportResult> ImportAsync(string filePath, ImportOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Import from a stream
    /// </summary>
    Task<ImportResult> ImportAsync(Stream stream, string fileName, ImportOptions? options = null, CancellationToken cancellationToken = default);
}
