using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArtStudio.Core;

namespace ArtStudio.CLI.Services;

/// <summary>
/// Headless implementation of IEditorService for CLI scenarios
/// </summary>
public class HeadlessEditorService : IEditorService
{
    private bool _hasContent;

    /// <inheritdoc />
    public bool HasContent => _hasContent;

    /// <inheritdoc />
    public void CreateNewDocument()
    {
        _hasContent = true;
        // For CLI, we just track that content exists
    }

    /// <inheritdoc />
    public void OpenDocument(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        _hasContent = true;
        // For CLI, we just track that content exists
    }

    /// <inheritdoc />
    public void SaveDocument()
    {
        // For CLI, this is a no-op unless we have a specific document path
        // Real saving would be handled by specific commands
    }

    /// <inheritdoc />
    public void SaveDocumentAs(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        // For CLI, this is a no-op - real saving would be handled by specific commands
        // We could implement basic file operations here if needed
    }

    /// <inheritdoc />
    public void StartDrawing()
    {
        _hasContent = true;
        // For CLI, we just track that drawing operations have started
    }

    /// <inheritdoc />
    public void ClearCanvas()
    {
        _hasContent = false;
        // For CLI, we just track that content has been cleared
    }
}
