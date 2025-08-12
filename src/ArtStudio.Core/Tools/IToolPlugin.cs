using System;
using System.Collections.Generic;

namespace ArtStudio.Core;

/// <summary>
/// Interface for drawing/editing tool plugins
/// </summary>
public interface IToolPlugin : IPlugin
{
    /// <summary>
    /// Tool category for organization
    /// </summary>
    ToolCategory Category { get; }

    /// <summary>
    /// Icon resource path or identifier
    /// </summary>
    string? IconResource { get; }

    /// <summary>
    /// Keyboard shortcut as string (e.g., "Ctrl+B")
    /// </summary>
    string? Shortcut { get; }

    /// <summary>
    /// Cursor type to display when tool is active
    /// </summary>
    ToolCursorType CursorType { get; }

    /// <summary>
    /// Tool settings/properties
    /// </summary>
    IToolSettings Settings { get; }

    /// <summary>
    /// Called when tool is selected
    /// </summary>
    void Activate();

    /// <summary>
    /// Called when tool is deselected
    /// </summary>
    void Deactivate();


    /// <summary>
    /// Get tool-specific context menu items
    /// </summary>
    IEnumerable<ToolMenuItem>? GetContextMenuItems();
}
