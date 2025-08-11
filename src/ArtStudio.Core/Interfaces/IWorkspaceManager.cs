using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtStudio.Core;

/// <summary>
/// Interface for managing workspaces
/// </summary>
public interface IWorkspaceManager
{
    /// <summary>
    /// Event fired when the active workspace changes
    /// </summary>
    event EventHandler<WorkspaceChangedEventArgs>? WorkspaceChanged;

    /// <summary>
    /// Event fired when a workspace is created
    /// </summary>
    event EventHandler<WorkspaceCreatedEventArgs>? WorkspaceCreated;

    /// <summary>
    /// Event fired when a workspace is deleted
    /// </summary>
    event EventHandler<WorkspaceDeletedEventArgs>? WorkspaceDeleted;

    /// <summary>
    /// Event fired when a workspace is modified
    /// </summary>
    event EventHandler<WorkspaceModifiedEventArgs>? WorkspaceModified;

    /// <summary>
    /// Currently active workspace
    /// </summary>
    WorkspaceConfiguration? ActiveWorkspace { get; }

    /// <summary>
    /// Get all available workspaces
    /// </summary>
    IEnumerable<WorkspaceConfiguration> GetWorkspaces();

    /// <summary>
    /// Get a workspace by ID
    /// </summary>
    WorkspaceConfiguration? GetWorkspace(string workspaceId);

    /// <summary>
    /// Create a new workspace
    /// </summary>
    Task<WorkspaceConfiguration> CreateWorkspaceAsync(string name, string description = "");

    /// <summary>
    /// Create a workspace from the current layout
    /// </summary>
    Task<WorkspaceConfiguration> CreateWorkspaceFromCurrentAsync(string name, string description = "");

    /// <summary>
    /// Save workspace configuration
    /// </summary>
    Task SaveWorkspaceAsync(WorkspaceConfiguration workspace);

    /// <summary>
    /// Delete a workspace
    /// </summary>
    Task<bool> DeleteWorkspaceAsync(string workspaceId);

    /// <summary>
    /// Switch to a workspace
    /// </summary>
    Task<bool> SwitchToWorkspaceAsync(string workspaceId);

    /// <summary>
    /// Duplicate a workspace
    /// </summary>
    Task<WorkspaceConfiguration> DuplicateWorkspaceAsync(string workspaceId, string newName);

    /// <summary>
    /// Reset workspace to default configuration
    /// </summary>
    Task ResetWorkspaceAsync(string workspaceId);

    /// <summary>
    /// Update current workspace with current layout
    /// </summary>
    Task UpdateCurrentWorkspaceAsync();

    /// <summary>
    /// Get built-in workspaces
    /// </summary>
    IEnumerable<WorkspaceConfiguration> GetBuiltInWorkspaces();

    /// <summary>
    /// Get custom workspaces
    /// </summary>
    IEnumerable<WorkspaceConfiguration> GetCustomWorkspaces();

    /// <summary>
    /// Initialize workspace system with default workspaces
    /// </summary>
    Task InitializeAsync();
}

/// <summary>
/// Interface for workspace layout management
/// </summary>
public interface IWorkspaceLayoutManager
{
    /// <summary>
    /// Apply a workspace configuration to the current layout
    /// </summary>
    Task ApplyWorkspaceAsync(WorkspaceConfiguration workspace);

    /// <summary>
    /// Capture current layout as workspace configuration
    /// </summary>
    Task<WorkspaceConfiguration> CaptureCurrentLayoutAsync(string name, string description = "");

    /// <summary>
    /// Add a panel to the current workspace
    /// </summary>
    Task AddPanelAsync(PanelConfiguration panel);

    /// <summary>
    /// Remove a panel from the current workspace
    /// </summary>
    Task RemovePanelAsync(string panelId);

    /// <summary>
    /// Move a panel to a new position
    /// </summary>
    Task MovePanelAsync(string panelId, PanelPosition newPosition);

    /// <summary>
    /// Resize a panel
    /// </summary>
    Task ResizePanelAsync(string panelId, PanelSize newSize);

    /// <summary>
    /// Show/hide a panel
    /// </summary>
    Task SetPanelVisibilityAsync(string panelId, bool isVisible);

    /// <summary>
    /// Add a toolbar to the workspace
    /// </summary>
    Task AddToolbarAsync(ToolbarConfiguration toolbar);

    /// <summary>
    /// Remove a toolbar from the workspace
    /// </summary>
    Task RemoveToolbarAsync(string toolbarId);

    /// <summary>
    /// Move a toolbar to a new position
    /// </summary>
    Task MoveToolbarAsync(string toolbarId, ToolbarPosition newPosition, int order);

    /// <summary>
    /// Add a command to a toolbar
    /// </summary>
    Task AddToolbarCommandAsync(string toolbarId, ToolbarItem item);

    /// <summary>
    /// Remove a command from a toolbar
    /// </summary>
    Task RemoveToolbarCommandAsync(string toolbarId, string commandId);

    /// <summary>
    /// Reorder commands in a toolbar
    /// </summary>
    Task ReorderToolbarCommandsAsync(string toolbarId, IList<string> commandOrder);

    /// <summary>
    /// Show/hide a toolbar
    /// </summary>
    Task SetToolbarVisibilityAsync(string toolbarId, bool isVisible);

    /// <summary>
    /// Reset layout to default
    /// </summary>
    Task ResetToDefaultLayoutAsync();
}

/// <summary>
/// Event arguments for workspace changed events
/// </summary>
public class WorkspaceChangedEventArgs : EventArgs
{
    public WorkspaceConfiguration? PreviousWorkspace { get; init; }
    public WorkspaceConfiguration CurrentWorkspace { get; init; } = null!;
}

/// <summary>
/// Event arguments for workspace created events
/// </summary>
public class WorkspaceCreatedEventArgs : EventArgs
{
    public WorkspaceConfiguration Workspace { get; init; } = null!;
}

/// <summary>
/// Event arguments for workspace deleted events
/// </summary>
public class WorkspaceDeletedEventArgs : EventArgs
{
    public string WorkspaceId { get; init; } = string.Empty;
    public string WorkspaceName { get; init; } = string.Empty;
}

/// <summary>
/// Event arguments for workspace modified events
/// </summary>
public class WorkspaceModifiedEventArgs : EventArgs
{
    public WorkspaceConfiguration Workspace { get; init; } = null!;
    public WorkspaceModificationType ModificationType { get; init; }
}

/// <summary>
/// Types of workspace modifications
/// </summary>
public enum WorkspaceModificationType
{
    PanelAdded,
    PanelRemoved,
    PanelMoved,
    PanelResized,
    PanelVisibilityChanged,
    ToolbarAdded,
    ToolbarRemoved,
    ToolbarMoved,
    ToolbarVisibilityChanged,
    ToolbarCommandAdded,
    ToolbarCommandRemoved,
    ToolbarCommandReordered,
    LayoutChanged,
    SettingsChanged
}
