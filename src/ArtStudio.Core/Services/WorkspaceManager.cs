using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ArtStudio.Core.Services;

/// <summary>
/// Implementation of workspace management
/// </summary>
public class WorkspaceManager : IWorkspaceManager
{
    private readonly ILogger<WorkspaceManager>? _logger;
    private readonly IWorkspaceLayoutManager _layoutManager;
    private readonly string _workspacesDirectory;
    private readonly Dictionary<string, WorkspaceConfiguration> _workspaces = new();
    private WorkspaceConfiguration? _activeWorkspace;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // High-performance logging delegates
    private static readonly Action<ILogger, string, string, Exception?> LogWorkspaceCreated =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(1, nameof(LogWorkspaceCreated)),
            "Created new workspace: {WorkspaceName} ({WorkspaceId})");

    private static readonly Action<ILogger, string, string, Exception?> LogWorkspaceLoaded =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(2, nameof(LogWorkspaceLoaded)),
            "Loaded workspace: {WorkspaceName} ({WorkspaceId})");

    private static readonly Action<ILogger, string, Exception?> LogStartingWorkspaceSwitch =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(3, nameof(LogStartingWorkspaceSwitch)),
            "Starting workspace switch to: {WorkspaceId}");

    private static readonly Action<ILogger, string, Exception?> LogWorkspaceNotFound =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(4, nameof(LogWorkspaceNotFound)),
            "Workspace not found: {WorkspaceId}");

    private static readonly Action<ILogger, string, Exception?> LogWorkspaceAlreadyActive =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(5, nameof(LogWorkspaceAlreadyActive)),
            "Workspace is already active: {WorkspaceId}");

    private static readonly Action<ILogger, string, Exception?> LogWorkspaceSwitched =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(6, nameof(LogWorkspaceSwitched)),
            "Successfully switched to workspace: {WorkspaceId}");

    private static readonly Action<ILogger, string, Exception?> LogWorkspaceUpdated =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(7, nameof(LogWorkspaceUpdated)),
            "Updated workspace: {WorkspaceId}");

    private static readonly Action<ILogger, string, Exception?> LogFailedToSwitchWorkspace =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(8, nameof(LogFailedToSwitchWorkspace)),
            "Failed to switch to workspace: {WorkspaceId}");

    private static readonly Action<ILogger, Exception?> LogLoadingWorkspaces =
        LoggerMessage.Define(LogLevel.Information, new EventId(9, nameof(LogLoadingWorkspaces)),
            "Loading workspaces...");

    private static readonly Action<ILogger, string, Exception?> LogWorkspaceDeleted =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(10, nameof(LogWorkspaceDeleted)),
            "Deleted workspace: {WorkspaceId}");

    private static readonly Action<ILogger, string, Exception?> LogWorkspaceDuplicated =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(11, nameof(LogWorkspaceDuplicated)),
            "Duplicated workspace: {WorkspaceId}");

    private static readonly Action<ILogger, string, Exception?> LogWorkspaceExported =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(12, nameof(LogWorkspaceExported)),
            "Exported workspace: {WorkspaceId}");

    private static readonly Action<ILogger, string, Exception?> LogLoadingWorkspaceFile =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(13, nameof(LogLoadingWorkspaceFile)),
            "Loading workspace from file: {FilePath}");

    private static readonly Action<ILogger, string, Exception?> LogFailedToLoadWorkspace =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(14, nameof(LogFailedToLoadWorkspace)),
            "Failed to load workspace: {FilePath}");

    private static readonly Action<ILogger, string, Exception?> LogSavingWorkspace =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(15, nameof(LogSavingWorkspace)),
            "Saving workspace: {WorkspaceId}");

    private static readonly Action<ILogger, string, string, Exception?> LogSavedWorkspace =
        LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(16, nameof(LogSavedWorkspace)),
            "Saved workspace: {WorkspaceName} to {FilePath}");

    private static readonly Action<ILogger, string, Exception?> LogCannotDeleteBuiltInWorkspace =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(17, nameof(LogCannotDeleteBuiltInWorkspace)),
            "Cannot delete built-in workspace: {WorkspaceName}");

    private static readonly Action<ILogger, string, Exception?> LogCannotDeleteActiveWorkspace =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(18, nameof(LogCannotDeleteActiveWorkspace)),
            "Cannot delete active workspace: {WorkspaceName}");

    private static readonly Action<ILogger, string, Exception?> LogWorkspaceReset =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(19, nameof(LogWorkspaceReset)),
            "Reset workspace: {WorkspaceId}");

    private static readonly Action<ILogger, string, Exception?> LogWorkspaceUpdatedCurrent =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(20, nameof(LogWorkspaceUpdatedCurrent)),
            "Updated current workspace: {WorkspaceName}");

    private static readonly Action<ILogger, Exception?> LogInitializingWorkspaceManager =
        LoggerMessage.Define(LogLevel.Information, new EventId(21, nameof(LogInitializingWorkspaceManager)),
            "Initializing workspace manager...");

    private static readonly Action<ILogger, int, Exception?> LogWorkspaceManagerInitialized =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(22, nameof(LogWorkspaceManagerInitialized)),
            "Workspace manager initialized with {Count} workspaces");

    private static readonly Action<ILogger, string, string, Exception?> LogLoadedWorkspaceFromFile =
        LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(23, nameof(LogLoadedWorkspaceFromFile)),
            "Loaded workspace: {WorkspaceName} from {FileName}");

    private static readonly Action<ILogger, string, Exception?> LogFailedToLoadWorkspaceFromFile =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(24, nameof(LogFailedToLoadWorkspaceFromFile)),
            "Failed to load workspace: {FilePath}");

    private static readonly Action<ILogger, string, Exception?> LogSavedWorkspaceToFile =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(25, nameof(LogSavedWorkspaceToFile)),
            "Saved workspace: {WorkspaceId}");

    public WorkspaceManager(
        IWorkspaceLayoutManager layoutManager,
        ILogger<WorkspaceManager>? logger = null)
    {
        _layoutManager = layoutManager ?? throw new ArgumentNullException(nameof(layoutManager));
        _logger = logger;

        // Set up workspaces directory
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _workspacesDirectory = Path.Combine(appDataPath, "ArtStudio", "Workspaces");
        Directory.CreateDirectory(_workspacesDirectory);
    }

    /// <inheritdoc />
    public event EventHandler<WorkspaceChangedEventArgs>? WorkspaceChanged;

    /// <inheritdoc />
    public event EventHandler<WorkspaceCreatedEventArgs>? WorkspaceCreated;

    /// <inheritdoc />
    public event EventHandler<WorkspaceDeletedEventArgs>? WorkspaceDeleted;

    /// <inheritdoc />
    public event EventHandler<WorkspaceModifiedEventArgs>? WorkspaceModified;

    /// <inheritdoc />
    public WorkspaceConfiguration? ActiveWorkspace => _activeWorkspace;

    /// <inheritdoc />
    public IEnumerable<WorkspaceConfiguration> GetWorkspaces()
    {
        return _workspaces.Values.OrderBy(w => w.IsBuiltIn ? 0 : 1).ThenBy(w => w.Name);
    }

    /// <inheritdoc />
    public WorkspaceConfiguration? GetWorkspace(string workspaceId)
    {
        if (string.IsNullOrWhiteSpace(workspaceId))
            return null;

        _workspaces.TryGetValue(workspaceId, out var workspace);
        return workspace;
    }

    /// <inheritdoc />
    public async Task<WorkspaceConfiguration> CreateWorkspaceAsync(string name, string description = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workspace name cannot be empty", nameof(name));

        var workspace = new WorkspaceConfiguration
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = description,
            IsBuiltIn = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        // Add default panels and toolbars
        AddDefaultPanels(workspace);
        AddDefaultToolbars(workspace);

        _workspaces[workspace.Id] = workspace;
        await SaveWorkspaceAsync(workspace).ConfigureAwait(false);

        if (_logger != null)
            LogWorkspaceCreated(_logger, name, workspace.Id, null);
        WorkspaceCreated?.Invoke(this, new WorkspaceCreatedEventArgs { Workspace = workspace });

        return workspace;
    }

    /// <inheritdoc />
    public async Task<WorkspaceConfiguration> CreateWorkspaceFromCurrentAsync(string name, string description = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workspace name cannot be empty", nameof(name));

        var workspace = await _layoutManager.CaptureCurrentLayoutAsync(name, description).ConfigureAwait(false);
        workspace.Id = Guid.NewGuid().ToString();
        workspace.IsBuiltIn = false;
        workspace.CreatedAt = DateTime.UtcNow;
        workspace.ModifiedAt = DateTime.UtcNow;

        _workspaces[workspace.Id] = workspace;
        await SaveWorkspaceAsync(workspace).ConfigureAwait(false);

        if (_logger != null)
            LogWorkspaceCreated(_logger, name, workspace.Id, null);
        WorkspaceCreated?.Invoke(this, new WorkspaceCreatedEventArgs { Workspace = workspace });

        return workspace;
    }

    /// <inheritdoc />
    public async Task SaveWorkspaceAsync(WorkspaceConfiguration workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        workspace.ModifiedAt = DateTime.UtcNow;

        var filePath = Path.Combine(_workspacesDirectory, $"{workspace.Id}.json");
        var json = JsonSerializer.Serialize(workspace, JsonOptions);

        await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);
        if (_logger != null)
            LogSavedWorkspace(_logger, workspace.Name, filePath, null);
    }

    /// <inheritdoc />
    public Task<bool> DeleteWorkspaceAsync(string workspaceId)
    {
        if (string.IsNullOrWhiteSpace(workspaceId))
            return Task.FromResult(false);

        var workspace = GetWorkspace(workspaceId);
        if (workspace == null)
            return Task.FromResult(false);

        if (workspace.IsBuiltIn)
        {
            if (_logger != null)
                LogCannotDeleteBuiltInWorkspace(_logger, workspace.Name, null);
            return Task.FromResult(false);
        }

        // Don't delete the active workspace
        if (_activeWorkspace?.Id == workspaceId)
        {
            if (_logger != null)
                LogCannotDeleteActiveWorkspace(_logger, workspace.Name, null);
            return Task.FromResult(false);
        }

        _workspaces.Remove(workspaceId);

        var filePath = Path.Combine(_workspacesDirectory, $"{workspaceId}.json");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        if (_logger != null)
            LogWorkspaceDeleted(_logger, workspaceId, null);
        WorkspaceDeleted?.Invoke(this, new WorkspaceDeletedEventArgs
        {
            WorkspaceId = workspaceId,
            WorkspaceName = workspace.Name
        });

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public async Task<bool> SwitchToWorkspaceAsync(string workspaceId)
    {
        if (string.IsNullOrWhiteSpace(workspaceId))
            return false;

        var workspace = GetWorkspace(workspaceId);
        if (workspace == null)
        {
            if (_logger != null)
                LogWorkspaceNotFound(_logger, workspaceId, null);
            return false;
        }

        var previousWorkspace = _activeWorkspace;

        try
        {
            await _layoutManager.ApplyWorkspaceAsync(workspace).ConfigureAwait(false);
            _activeWorkspace = workspace;

            if (_logger != null)
                LogWorkspaceSwitched(_logger, workspaceId, null);
            WorkspaceChanged?.Invoke(this, new WorkspaceChangedEventArgs
            {
                PreviousWorkspace = previousWorkspace,
                CurrentWorkspace = workspace
            });

            return true;
        }
        catch (Exception ex)
        {
            if (_logger != null)
                LogFailedToSwitchWorkspace(_logger, workspaceId, ex);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<WorkspaceConfiguration> DuplicateWorkspaceAsync(string workspaceId, string newName)
    {
        var sourceWorkspace = GetWorkspace(workspaceId);
        if (sourceWorkspace == null)
            throw new ArgumentException($"Workspace not found: {workspaceId}", nameof(workspaceId));

        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("New workspace name cannot be empty", nameof(newName));

        var newWorkspace = new WorkspaceConfiguration
        {
            Id = Guid.NewGuid().ToString(),
            Name = newName,
            Description = $"Copy of {sourceWorkspace.Name}",
            IconResource = sourceWorkspace.IconResource,
            IsBuiltIn = false,
            LayoutData = sourceWorkspace.LayoutData,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        // Copy panels
        foreach (var p in sourceWorkspace.Panels)
        {
            newWorkspace.Panels.Add(new PanelConfiguration
            {
                Id = p.Id,
                Name = p.Name,
                Type = p.Type,
                IsVisible = p.IsVisible,
                Position = new PanelPosition
                {
                    DockSide = p.Position.DockSide,
                    IsFloating = p.Position.IsFloating,
                    FloatingPosition = new Point { X = p.Position.FloatingPosition.X, Y = p.Position.FloatingPosition.Y },
                    Order = p.Position.Order,
                    PaneGroup = p.Position.PaneGroup
                },
                Size = new PanelSize
                {
                    Width = p.Size.Width,
                    Height = p.Size.Height,
                    MinWidth = p.Size.MinWidth,
                    MinHeight = p.Size.MinHeight,
                    MaxWidth = p.Size.MaxWidth,
                    MaxHeight = p.Size.MaxHeight
                },
                CanClose = p.CanClose,
                CanHide = p.CanHide,
                CanFloat = p.CanFloat
            });

            // Copy settings individually
            foreach (var setting in p.Settings)
            {
                newWorkspace.Panels.Last().Settings.Add(setting.Key, setting.Value);
            }
        }

        // Copy toolbars
        foreach (var t in sourceWorkspace.Toolbars)
        {
            var toolbarConfig = new ToolbarConfiguration
            {
                Id = t.Id,
                Name = t.Name,
                IsVisible = t.IsVisible,
                Position = t.Position,
                Order = t.Order,
                CanMove = t.CanMove,
                CanHide = t.CanHide
            };

            foreach (var i in t.Items)
            {
                toolbarConfig.Items.Add(new ToolbarItem
                {
                    Type = i.Type,
                    CommandId = i.CommandId,
                    Text = i.Text,
                    IconResource = i.IconResource,
                    ToolTip = i.ToolTip,
                    IsVisible = i.IsVisible,
                    IsEnabled = i.IsEnabled,
                    Order = i.Order
                });
            }

            newWorkspace.Toolbars.Add(toolbarConfig);
        }

        _workspaces[newWorkspace.Id] = newWorkspace;
        await SaveWorkspaceAsync(newWorkspace).ConfigureAwait(false);

        if (_logger != null)
            LogWorkspaceDuplicated(_logger, newWorkspace.Id, null);
        WorkspaceCreated?.Invoke(this, new WorkspaceCreatedEventArgs { Workspace = newWorkspace });

        return newWorkspace;
    }

    /// <inheritdoc />
    public async Task ResetWorkspaceAsync(string workspaceId)
    {
        var workspace = GetWorkspace(workspaceId);
        if (workspace == null)
            throw new ArgumentException($"Workspace not found: {workspaceId}", nameof(workspaceId));

        // Clear existing configuration
        workspace.Panels.Clear();
        workspace.Toolbars.Clear();
        workspace.LayoutData = null;

        // Add default configuration
        if (workspace.IsBuiltIn)
        {
            ConfigureBuiltInWorkspace(workspace);
        }
        else
        {
            AddDefaultPanels(workspace);
            AddDefaultToolbars(workspace);
        }

        workspace.ModifiedAt = DateTime.UtcNow;
        await SaveWorkspaceAsync(workspace).ConfigureAwait(false);

        // If this is the active workspace, apply the changes
        if (_activeWorkspace?.Id == workspaceId)
        {
            await _layoutManager.ApplyWorkspaceAsync(workspace).ConfigureAwait(false);
        }

        if (_logger != null)
            LogWorkspaceReset(_logger, workspaceId, null);
        WorkspaceModified?.Invoke(this, new WorkspaceModifiedEventArgs
        {
            Workspace = workspace,
            ModificationType = WorkspaceModificationType.LayoutChanged
        });
    }

    /// <inheritdoc />
    public async Task UpdateCurrentWorkspaceAsync()
    {
        if (_activeWorkspace == null)
            return;

        var updatedWorkspace = await _layoutManager.CaptureCurrentLayoutAsync(_activeWorkspace.Name, _activeWorkspace.Description).ConfigureAwait(false);

        // Preserve original metadata
        updatedWorkspace.Id = _activeWorkspace.Id;
        updatedWorkspace.IsBuiltIn = _activeWorkspace.IsBuiltIn;
        updatedWorkspace.CreatedAt = _activeWorkspace.CreatedAt;
        updatedWorkspace.ModifiedAt = DateTime.UtcNow;

        _workspaces[_activeWorkspace.Id] = updatedWorkspace;
        _activeWorkspace = updatedWorkspace;

        await SaveWorkspaceAsync(_activeWorkspace).ConfigureAwait(false);

        if (_logger != null)
            LogWorkspaceUpdatedCurrent(_logger, _activeWorkspace.Name, null);
        WorkspaceModified?.Invoke(this, new WorkspaceModifiedEventArgs
        {
            Workspace = _activeWorkspace,
            ModificationType = WorkspaceModificationType.LayoutChanged
        });
    }

    /// <inheritdoc />
    public IEnumerable<WorkspaceConfiguration> GetBuiltInWorkspaces()
    {
        return _workspaces.Values.Where(w => w.IsBuiltIn).OrderBy(w => w.Name);
    }

    /// <inheritdoc />
    public IEnumerable<WorkspaceConfiguration> GetCustomWorkspaces()
    {
        return _workspaces.Values.Where(w => !w.IsBuiltIn).OrderBy(w => w.Name);
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (_logger != null)
            LogInitializingWorkspaceManager(_logger, null);

        // Load existing workspaces
        await LoadWorkspacesAsync().ConfigureAwait(false);

        // Create built-in workspaces if they don't exist
        await CreateBuiltInWorkspacesAsync().ConfigureAwait(false);

        // Set default active workspace if none is set
        if (_activeWorkspace == null)
        {
            var defaultWorkspace = GetBuiltInWorkspaces().FirstOrDefault(w => w.Name == "Drawing");
            if (defaultWorkspace != null)
            {
                await SwitchToWorkspaceAsync(defaultWorkspace.Id).ConfigureAwait(false);
            }
        }

        if (_logger != null)
            LogWorkspaceManagerInitialized(_logger, _workspaces.Count, null);
    }

    private async Task LoadWorkspacesAsync()
    {
        if (!Directory.Exists(_workspacesDirectory))
            return;

        var workspaceFiles = Directory.GetFiles(_workspacesDirectory, "*.json");

        foreach (var file in workspaceFiles)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file).ConfigureAwait(false);
                var workspace = JsonSerializer.Deserialize<WorkspaceConfiguration>(json, JsonOptions);

                if (workspace != null && !string.IsNullOrWhiteSpace(workspace.Id))
                {
                    _workspaces[workspace.Id] = workspace;
                    if (_logger != null)
                        LogLoadedWorkspaceFromFile(_logger, workspace.Name, Path.GetFileName(file), null);
                }
            }
            catch (Exception ex)
            {
                if (_logger != null)
                    LogFailedToLoadWorkspaceFromFile(_logger, Path.GetFileName(file), ex);
                throw;
            }
        }
    }

    private async Task CreateBuiltInWorkspacesAsync()
    {
        var builtInWorkspaces = new[]
        {
            ("Drawing", "Optimized for digital drawing and sketching", "Brush"),
            ("Photo Editing", "Designed for photo editing and retouching", "Image"),
            ("Compositing", "Ideal for compositing and layer work", "Layers")
        };

        foreach (var (name, description, icon) in builtInWorkspaces)
        {
            var existingWorkspace = _workspaces.Values.FirstOrDefault(w => w.IsBuiltIn && w.Name == name);
            if (existingWorkspace == null)
            {
                var workspace = new WorkspaceConfiguration
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Description = description,
                    IconResource = icon,
                    IsBuiltIn = true,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                };

                ConfigureBuiltInWorkspace(workspace);
                _workspaces[workspace.Id] = workspace;
                await SaveWorkspaceAsync(workspace).ConfigureAwait(false);

                if (_logger != null)
                    LogSavedWorkspaceToFile(_logger, name, null);
            }
        }
    }

    private static void ConfigureBuiltInWorkspace(WorkspaceConfiguration workspace)
    {
        switch (workspace.Name)
        {
            case "Drawing":
                ConfigureDrawingWorkspace(workspace);
                break;
            case "Photo Editing":
                ConfigurePhotoEditingWorkspace(workspace);
                break;
            case "Compositing":
                ConfigureCompositingWorkspace(workspace);
                break;
            default:
                AddDefaultPanels(workspace);
                AddDefaultToolbars(workspace);
                break;
        }
    }

    private static void ConfigureDrawingWorkspace(WorkspaceConfiguration workspace)
    {
        // Tool palette on the left
        workspace.Panels.Add(new PanelConfiguration
        {
            Id = "toolPalette",
            Name = "Tools",
            Type = PanelType.ToolPalette,
            IsVisible = true,
            Position = new PanelPosition { DockSide = DockSide.Left, Order = 0 },
            Size = new PanelSize { Width = 80, MinWidth = 60, MaxWidth = 120 }
        });

        // Brush settings on the left below tools
        workspace.Panels.Add(new PanelConfiguration
        {
            Id = "brushSettings",
            Name = "Brush Settings",
            Type = PanelType.BrushSettings,
            IsVisible = true,
            Position = new PanelPosition { DockSide = DockSide.Left, Order = 1 },
            Size = new PanelSize { Width = 200, MinWidth = 150 }
        });

        // Color picker on the right
        workspace.Panels.Add(new PanelConfiguration
        {
            Id = "colorPicker",
            Name = "Colors",
            Type = PanelType.ColorPicker,
            IsVisible = true,
            Position = new PanelPosition { DockSide = DockSide.Right, Order = 0 },
            Size = new PanelSize { Width = 250, MinWidth = 200 }
        });

        // Layers on the right below colors
        workspace.Panels.Add(new PanelConfiguration
        {
            Id = "layerPanel",
            Name = "Layers",
            Type = PanelType.LayerPanel,
            IsVisible = true,
            Position = new PanelPosition { DockSide = DockSide.Right, Order = 1 },
            Size = new PanelSize { Width = 250, MinWidth = 200 }
        });

        AddDrawingToolbars(workspace);
    }

    private static void ConfigurePhotoEditingWorkspace(WorkspaceConfiguration workspace)
    {
        // Navigator on the left
        workspace.Panels.Add(new PanelConfiguration
        {
            Id = "navigator",
            Name = "Navigator",
            Type = PanelType.Navigator,
            IsVisible = true,
            Position = new PanelPosition { DockSide = DockSide.Left, Order = 0 },
            Size = new PanelSize { Width = 200, MinWidth = 150 }
        });

        // History on the left below navigator
        workspace.Panels.Add(new PanelConfiguration
        {
            Id = "history",
            Name = "History",
            Type = PanelType.History,
            IsVisible = true,
            Position = new PanelPosition { DockSide = DockSide.Left, Order = 1 },
            Size = new PanelSize { Width = 200, MinWidth = 150 }
        });

        // Layers on the right
        workspace.Panels.Add(new PanelConfiguration
        {
            Id = "layerPanel",
            Name = "Layers",
            Type = PanelType.LayerPanel,
            IsVisible = true,
            Position = new PanelPosition { DockSide = DockSide.Right, Order = 0 },
            Size = new PanelSize { Width = 300, MinWidth = 250 }
        });

        // Properties on the right below layers
        workspace.Panels.Add(new PanelConfiguration
        {
            Id = "properties",
            Name = "Properties",
            Type = PanelType.Properties,
            IsVisible = true,
            Position = new PanelPosition { DockSide = DockSide.Right, Order = 1 },
            Size = new PanelSize { Width = 300, MinWidth = 250 }
        });

        AddPhotoEditingToolbars(workspace);
    }

    private static void ConfigureCompositingWorkspace(WorkspaceConfiguration workspace)
    {
        // Layers on the left (prominent for compositing)
        workspace.Panels.Add(new PanelConfiguration
        {
            Id = "layerPanel",
            Name = "Layers",
            Type = PanelType.LayerPanel,
            IsVisible = true,
            Position = new PanelPosition { DockSide = DockSide.Left, Order = 0 },
            Size = new PanelSize { Width = 350, MinWidth = 300 }
        });

        // Properties on the right
        workspace.Panels.Add(new PanelConfiguration
        {
            Id = "properties",
            Name = "Properties",
            Type = PanelType.Properties,
            IsVisible = true,
            Position = new PanelPosition { DockSide = DockSide.Right, Order = 0 },
            Size = new PanelSize { Width = 300, MinWidth = 250 }
        });

        // Color picker on the right below properties
        workspace.Panels.Add(new PanelConfiguration
        {
            Id = "colorPicker",
            Name = "Colors",
            Type = PanelType.ColorPicker,
            IsVisible = true,
            Position = new PanelPosition { DockSide = DockSide.Right, Order = 1 },
            Size = new PanelSize { Width = 300, MinWidth = 250 }
        });

        // Tool palette as a smaller panel on the right
        workspace.Panels.Add(new PanelConfiguration
        {
            Id = "toolPalette",
            Name = "Tools",
            Type = PanelType.ToolPalette,
            IsVisible = true,
            Position = new PanelPosition { DockSide = DockSide.Right, Order = 2 },
            Size = new PanelSize { Width = 80, MinWidth = 60, MaxWidth = 120 }
        });

        AddCompositingToolbars(workspace);
    }

    private static void AddDefaultPanels(WorkspaceConfiguration workspace)
    {
        // Standard panel configuration
        var panels = new[]
        {
            new PanelConfiguration
            {
                Id = "toolPalette",
                Name = "Tools",
                Type = PanelType.ToolPalette,
                IsVisible = true,
                Position = new PanelPosition { DockSide = DockSide.Left, Order = 0 },
                Size = new PanelSize { Width = 200, MinWidth = 150 }
            },
            new PanelConfiguration
            {
                Id = "layerPanel",
                Name = "Layers",
                Type = PanelType.LayerPanel,
                IsVisible = true,
                Position = new PanelPosition { DockSide = DockSide.Right, Order = 0 },
                Size = new PanelSize { Width = 250, MinWidth = 200 }
            },
            new PanelConfiguration
            {
                Id = "properties",
                Name = "Properties",
                Type = PanelType.Properties,
                IsVisible = true,
                Position = new PanelPosition { DockSide = DockSide.Right, Order = 1 },
                Size = new PanelSize { Width = 250, MinWidth = 200 }
            }
        };

        foreach (var panel in panels)
        {
            workspace.Panels.Add(panel);
        }
    }

    private static void AddDefaultToolbars(WorkspaceConfiguration workspace)
    {
        // Main toolbar
        var mainToolbar = new ToolbarConfiguration
        {
            Id = "mainToolbar",
            Name = "Main",
            IsVisible = true,
            Position = ToolbarPosition.Top,
            Order = 0
        };

        var mainItems = new[]
        {
            new ToolbarItem { Type = ToolbarItemType.Command, CommandId = "File.New", IconResource = "FileOutline", ToolTip = "New", Order = 0 },
            new ToolbarItem { Type = ToolbarItemType.Command, CommandId = "File.Open", IconResource = "FolderOpenOutline", ToolTip = "Open", Order = 1 },
            new ToolbarItem { Type = ToolbarItemType.Command, CommandId = "File.Save", IconResource = "ContentSave", ToolTip = "Save", Order = 2 },
            new ToolbarItem { Type = ToolbarItemType.Separator, Order = 3 },
            new ToolbarItem { Type = ToolbarItemType.Command, CommandId = "Edit.Undo", IconResource = "Undo", ToolTip = "Undo", Order = 4 },
            new ToolbarItem { Type = ToolbarItemType.Command, CommandId = "Edit.Redo", IconResource = "Redo", ToolTip = "Redo", Order = 5 }
        };

        foreach (var item in mainItems)
        {
            mainToolbar.Items.Add(item);
        }

        workspace.Toolbars.Add(mainToolbar);
    }

    private static void AddDrawingToolbars(WorkspaceConfiguration workspace)
    {
        AddDefaultToolbars(workspace);

        // Drawing-specific toolbar
        var drawingToolbar = new ToolbarConfiguration
        {
            Id = "drawingToolbar",
            Name = "Drawing",
            IsVisible = true,
            Position = ToolbarPosition.Top,
            Order = 1
        };

        var drawingItems = new[]
        {
            new ToolbarItem { Type = ToolbarItemType.Command, CommandId = "Tool.Brush", IconResource = "Brush", ToolTip = "Brush", Order = 0 },
            new ToolbarItem { Type = ToolbarItemType.Command, CommandId = "Tool.Pencil", IconResource = "Pencil", ToolTip = "Pencil", Order = 1 },
            new ToolbarItem { Type = ToolbarItemType.Command, CommandId = "Tool.Eraser", IconResource = "Eraser", ToolTip = "Eraser", Order = 2 },
            new ToolbarItem { Type = ToolbarItemType.Separator, Order = 3 },
            new ToolbarItem { Type = ToolbarItemType.Command, CommandId = "Tool.Line", IconResource = "VectorLine", ToolTip = "Line", Order = 4 },
            new ToolbarItem { Type = ToolbarItemType.Command, CommandId = "Tool.Rectangle", IconResource = "RectangleOutline", ToolTip = "Rectangle", Order = 5 },
            new ToolbarItem { Type = ToolbarItemType.Command, CommandId = "Tool.Ellipse", IconResource = "EllipseOutline", ToolTip = "Ellipse", Order = 6 }
        };

        foreach (var item in drawingItems)
        {
            drawingToolbar.Items.Add(item);
        }

        workspace.Toolbars.Add(drawingToolbar);
    }

    private static void AddPhotoEditingToolbars(WorkspaceConfiguration workspace)
    {
        AddDefaultToolbars(workspace);

        // Photo editing toolbar
        var photoToolbar = new ToolbarConfiguration
        {
            Id = "photoToolbar",
            Name = "Photo Editing",
            IsVisible = true,
            Position = ToolbarPosition.Top,
            Order = 1
        };

        var photoItems = new[]
        {
            new ToolbarItem { Type = ToolbarItemType.Command, CommandId = "Tool.Crop", IconResource = "Crop", ToolTip = "Crop", Order = 0 },
            new ToolbarItem { Type = ToolbarItemType.Command, CommandId = "Tool.Healing", IconResource = "Healing", ToolTip = "Healing Brush", Order = 1 },
            new ToolbarItem { Type = ToolbarItemType.Command, CommandId = "Tool.Clone", IconResource = "ContentCopy", ToolTip = "Clone", Order = 2 },
            new ToolbarItem { Type = ToolbarItemType.Separator, Order = 3 },
            new ToolbarItem { Type = ToolbarItemType.Command, CommandId = "Adjust.Brightness", IconResource = "Brightness6", ToolTip = "Brightness/Contrast", Order = 4 },
            new ToolbarItem { Type = ToolbarItemType.Command, CommandId = "Adjust.Levels", IconResource = "ChartLine", ToolTip = "Levels", Order = 5 },
            new ToolbarItem { Type = ToolbarItemType.Command, CommandId = "Adjust.Curves", IconResource = "ChartBellCurve", ToolTip = "Curves", Order = 6 }
        };

        foreach (var item in photoItems)
        {
            photoToolbar.Items.Add(item);
        }

        workspace.Toolbars.Add(photoToolbar);
    }

    private static void AddCompositingToolbars(WorkspaceConfiguration workspace)
    {
        AddDefaultToolbars(workspace);

        // Compositing toolbar
        var compositingToolbar = new ToolbarConfiguration
        {
            Id = "compositingToolbar",
            Name = "Compositing",
            IsVisible = true,
            Position = ToolbarPosition.Top,
            Order = 1
        };

        var compositingItems = new[]
        {
            new ToolbarItem { Type = ToolbarItemType.Command, CommandId = "Layer.Add", IconResource = "LayerPlus", ToolTip = "Add Layer", Order = 0 },
            new ToolbarItem { Type = ToolbarItemType.Command, CommandId = "Layer.Duplicate", IconResource = "LayersCopy", ToolTip = "Duplicate Layer", Order = 1 },
            new ToolbarItem { Type = ToolbarItemType.Command, CommandId = "Layer.Mask", IconResource = "VectorSquare", ToolTip = "Add Mask", Order = 2 },
            new ToolbarItem { Type = ToolbarItemType.Separator, Order = 3 },
            new ToolbarItem { Type = ToolbarItemType.Command, CommandId = "Tool.Selection", IconResource = "SelectionDrag", ToolTip = "Selection", Order = 4 },
            new ToolbarItem { Type = ToolbarItemType.Command, CommandId = "Tool.Transform", IconResource = "VectorSquare", ToolTip = "Transform", Order = 5 },
            new ToolbarItem { Type = ToolbarItemType.Command, CommandId = "Tool.Move", IconResource = "CursorMove", ToolTip = "Move", Order = 6 }
        };

        foreach (var item in compositingItems)
        {
            compositingToolbar.Items.Add(item);
        }

        workspace.Toolbars.Add(compositingToolbar);
    }
}
