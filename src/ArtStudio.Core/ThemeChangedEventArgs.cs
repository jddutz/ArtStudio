using System;

namespace ArtStudio.Core;

/// <summary>
/// Event arguments for theme changes
/// </summary>
public class ThemeChangedEventArgs : EventArgs
{
    public string PreviousTheme { get; }
    public string NewTheme { get; }

    public ThemeChangedEventArgs(string previousTheme, string newTheme)
    {
        PreviousTheme = previousTheme;
        NewTheme = newTheme;
    }
}
