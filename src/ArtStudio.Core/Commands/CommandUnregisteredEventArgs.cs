using System;

namespace ArtStudio.Core.Commands;

/// <summary>
/// Event arguments for command unregistration
/// </summary>
public class CommandUnregisteredEventArgs : EventArgs
{
    public string CommandId { get; }
    public IPluginCommand Command { get; }

    public CommandUnregisteredEventArgs(string commandId, IPluginCommand command)
    {
        CommandId = commandId;
        Command = command;
    }
}
