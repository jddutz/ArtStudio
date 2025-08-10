namespace ArtStudio.Core.Interfaces;

/// <summary>
/// Interface for editor functionality
/// </summary>
public interface IEditorService
{
    /// <summary>
    /// Create a new document
    /// </summary>
    void CreateNewDocument();

    /// <summary>
    /// Open an existing document
    /// </summary>
    void OpenDocument(string filePath);

    /// <summary>
    /// Save the current document
    /// </summary>
    void SaveDocument();

    /// <summary>
    /// Save the current document with a new name
    /// </summary>
    void SaveDocumentAs(string filePath);

    /// <summary>
    /// Start a new drawing operation
    /// </summary>
    void StartDrawing();

    /// <summary>
    /// Clear the editor canvas
    /// </summary>
    void ClearCanvas();

    /// <summary>
    /// Get current canvas state
    /// </summary>
    bool HasContent { get; }
}
