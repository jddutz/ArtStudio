using System;

namespace ArtStudio.Core;

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
