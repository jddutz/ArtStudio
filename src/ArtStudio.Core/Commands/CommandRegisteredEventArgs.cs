using System;

namespace ArtStudio.Core.Commands;

/// <summary>
/// Event arguments for command registration
/// </summary>
public class CommandRegisteredEventArgs : EventArgs
{
    public IPluginCommand Command { get; }

    public CommandRegisteredEventArgs(IPluginCommand command)
    {
        Command = command;
    }
}
