using System;

namespace ArtStudio.Core;

/// <summary>
/// Pointer event arguments for tool interactions
/// </summary>
public class PointerEventArgs : EventArgs
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Pressure { get; set; } = 1.0;
    public double Tilt { get; set; }
    public bool IsLeftButtonPressed { get; set; }
    public bool IsRightButtonPressed { get; set; }
    public bool IsMiddleButtonPressed { get; set; }
    public bool IsCtrlPressed { get; set; }
    public bool IsShiftPressed { get; set; }
    public bool IsAltPressed { get; set; }
    public PointerType PointerType { get; set; } = PointerType.Mouse;
}
