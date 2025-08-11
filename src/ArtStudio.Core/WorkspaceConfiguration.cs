using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ArtStudio.Core;

/// <summary>
/// Configuration for a workspace layout
/// </summary>
public class WorkspaceConfiguration
{
    /// <summary>
    /// Unique identifier for the workspace
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the workspace
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the workspace
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Icon resource for the workspace
    /// </summary>
    public string? IconResource { get; set; }

    /// <summary>
    /// Whether this is a built-in workspace
    /// </summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// Panel configurations for the workspace
    /// </summary>
    public Collection<PanelConfiguration> Panels { get; } = new();

    /// <summary>
    /// Toolbar configurations for the workspace
    /// </summary>
    public Collection<ToolbarConfiguration> Toolbars { get; } = new();

    /// <summary>
    /// Layout data (specific to the docking library)
    /// </summary>
    public string? LayoutData { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modified timestamp
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Configuration for a panel in a workspace
/// </summary>
public class PanelConfiguration
{
    /// <summary>
    /// Unique identifier for the panel
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the panel
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Panel type (e.g., "ToolPalette", "LayerPanel", "Properties")
    /// </summary>
    public PanelType Type { get; set; }

    /// <summary>
    /// Whether the panel is visible
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Panel position and docking configuration
    /// </summary>
    public PanelPosition Position { get; set; } = new();

    /// <summary>
    /// Panel size configuration
    /// </summary>
    public PanelSize Size { get; set; } = new();

    /// <summary>
    /// Whether the panel can be closed
    /// </summary>
    public bool CanClose { get; set; } = true;

    /// <summary>
    /// Whether the panel can be hidden
    /// </summary>
    public bool CanHide { get; set; } = true;

    /// <summary>
    /// Whether the panel can float
    /// </summary>
    public bool CanFloat { get; set; } = true;

    /// <summary>
    /// Panel-specific settings
    /// </summary>
    public Dictionary<string, object> Settings { get; } = new();
}

/// <summary>
/// Panel types supported by the workspace system
/// </summary>
public enum PanelType
{
    ToolPalette,
    LayerPanel,
    Properties,
    ColorPicker,
    BrushSettings,
    History,
    Navigator,
    Custom
}

/// <summary>
/// Panel position and docking information
/// </summary>
public class PanelPosition
{
    /// <summary>
    /// Docking side
    /// </summary>
    public DockSide DockSide { get; set; } = DockSide.Left;

    /// <summary>
    /// Whether the panel is floating
    /// </summary>
    public bool IsFloating { get; set; }

    /// <summary>
    /// Floating window position (if floating)
    /// </summary>
    public Point FloatingPosition { get; set; } = new();

    /// <summary>
    /// Order index within the dock side
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Pane group identifier (panels in same pane appear as tabs)
    /// </summary>
    public string? PaneGroup { get; set; }
}

/// <summary>
/// Docking sides for panels
/// </summary>
public enum DockSide
{
    Left,
    Right,
    Top,
    Bottom,
    Center,
    Floating
}

/// <summary>
/// Panel size configuration
/// </summary>
public class PanelSize
{
    /// <summary>
    /// Width (for left/right docked panels)
    /// </summary>
    public double Width { get; set; } = 200;

    /// <summary>
    /// Height (for top/bottom docked panels)
    /// </summary>
    public double Height { get; set; } = 200;

    /// <summary>
    /// Minimum width
    /// </summary>
    public double MinWidth { get; set; } = 100;

    /// <summary>
    /// Minimum height
    /// </summary>
    public double MinHeight { get; set; } = 100;

    /// <summary>
    /// Maximum width (0 = no limit)
    /// </summary>
    public double MaxWidth { get; set; }

    /// <summary>
    /// Maximum height (0 = no limit)
    /// </summary>
    public double MaxHeight { get; set; }
}

/// <summary>
/// Simple point structure
/// </summary>
public class Point
{
    public double X { get; set; }
    public double Y { get; set; }
}

/// <summary>
/// Configuration for a toolbar in a workspace
/// </summary>
public class ToolbarConfiguration
{
    /// <summary>
    /// Unique identifier for the toolbar
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the toolbar
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether the toolbar is visible
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Toolbar position
    /// </summary>
    public ToolbarPosition Position { get; set; } = ToolbarPosition.Top;

    /// <summary>
    /// Order index for toolbar positioning
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Commands included in this toolbar
    /// </summary>
    public Collection<ToolbarItem> Items { get; } = new();

    /// <summary>
    /// Whether the toolbar can be moved
    /// </summary>
    public bool CanMove { get; set; } = true;

    /// <summary>
    /// Whether the toolbar can be hidden
    /// </summary>
    public bool CanHide { get; set; } = true;
}

/// <summary>
/// Toolbar positions
/// </summary>
public enum ToolbarPosition
{
    Top,
    Bottom,
    Left,
    Right,
    Floating
}

/// <summary>
/// Item in a toolbar
/// </summary>
public class ToolbarItem
{
    /// <summary>
    /// Type of toolbar item
    /// </summary>
    public ToolbarItemType Type { get; set; }

    /// <summary>
    /// Command ID (for command items)
    /// </summary>
    public string? CommandId { get; set; }

    /// <summary>
    /// Display text (optional, for labeled buttons)
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Icon resource
    /// </summary>
    public string? IconResource { get; set; }

    /// <summary>
    /// Tooltip text
    /// </summary>
    public string? ToolTip { get; set; }

    /// <summary>
    /// Whether the item is visible
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Whether the item is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Order index within the toolbar
    /// </summary>
    public int Order { get; set; }
}

/// <summary>
/// Types of toolbar items
/// </summary>
public enum ToolbarItemType
{
    Command,
    Separator,
    Spacer,
    Custom
}
