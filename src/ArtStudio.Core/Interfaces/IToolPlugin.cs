using System;

namespace ArtStudio.Core.Interfaces;

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
    /// Handle mouse/stylus down event
    /// </summary>
    void OnPointerDown(PointerEventArgs e);

    /// <summary>
    /// Handle mouse/stylus move event
    /// </summary>
    void OnPointerMove(PointerEventArgs e);

    /// <summary>
    /// Handle mouse/stylus up event
    /// </summary>
    void OnPointerUp(PointerEventArgs e);

    /// <summary>
    /// Handle key down event
    /// </summary>
    void OnKeyDown(KeyEventArgs e);

    /// <summary>
    /// Handle key up event
    /// </summary>
    void OnKeyUp(KeyEventArgs e);

    /// <summary>
    /// Get tool-specific context menu items
    /// </summary>
    IEnumerable<ToolMenuItem>? GetContextMenuItems();
}

/// <summary>
/// Tool cursor types
/// </summary>
public enum ToolCursorType
{
    Default,
    Crosshair,
    Hand,
    Move,
    Resize,
    Text,
    Eyedropper,
    Custom
}

/// <summary>
/// Tool categories for organization
/// </summary>
public enum ToolCategory
{
    Selection,
    Drawing,
    Painting,
    Editing,
    Transform,
    Filter,
    Custom
}

/// <summary>
/// Interface for tool settings
/// </summary>
public interface IToolSettings
{
    /// <summary>
    /// Get all settings as key-value pairs
    /// </summary>
    Dictionary<string, object> GetSettings();

    /// <summary>
    /// Set a setting value
    /// </summary>
    void SetSetting(string key, object value);

    /// <summary>
    /// Get a setting value
    /// </summary>
    T? GetSetting<T>(string key, T? defaultValue = default);

    /// <summary>
    /// Reset all settings to defaults
    /// </summary>
    void ResetToDefaults();

    /// <summary>
    /// Event raised when settings change
    /// </summary>
    event EventHandler<SettingsChangedEventArgs> SettingsChanged;
}

/// <summary>
/// Pointer event arguments for tool interactions
/// </summary>
public class PointerEventArgs : EventArgs
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Pressure { get; set; } = 1.0;
    public double Tilt { get; set; } = 0.0;
    public bool IsLeftButtonPressed { get; set; }
    public bool IsRightButtonPressed { get; set; }
    public bool IsMiddleButtonPressed { get; set; }
    public bool IsCtrlPressed { get; set; }
    public bool IsShiftPressed { get; set; }
    public bool IsAltPressed { get; set; }
    public PointerType PointerType { get; set; } = PointerType.Mouse;
}

/// <summary>
/// Key event arguments for tool interactions
/// </summary>
public class KeyEventArgs : EventArgs
{
    public string Key { get; set; } = string.Empty;
    public bool IsCtrlPressed { get; set; }
    public bool IsShiftPressed { get; set; }
    public bool IsAltPressed { get; set; }
    public bool Handled { get; set; }
}

/// <summary>
/// Pointer type enumeration
/// </summary>
public enum PointerType
{
    Mouse,
    Stylus,
    Touch,
    Eraser
}

/// <summary>
/// Tool context menu item
/// </summary>
public class ToolMenuItem
{
    public string Header { get; set; } = string.Empty;
    public Action? Command { get; set; }
    public object? CommandParameter { get; set; }
    public string? IconResource { get; set; }
    public bool IsSeparator { get; set; }
    public List<ToolMenuItem>? SubItems { get; set; }
}

/// <summary>
/// Settings changed event arguments
/// </summary>
public class SettingsChangedEventArgs : EventArgs
{
    public string SettingKey { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }

    public SettingsChangedEventArgs(string settingKey, object? oldValue, object? newValue)
    {
        SettingKey = settingKey;
        OldValue = oldValue;
        NewValue = newValue;
    }
}
