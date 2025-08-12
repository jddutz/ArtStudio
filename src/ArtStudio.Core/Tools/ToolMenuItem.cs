using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ArtStudio.Core;

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
    public Collection<ToolMenuItem>? SubItems { get; } = new();
}
