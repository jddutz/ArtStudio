using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ArtStudio.Core;
using Microsoft.Extensions.Logging;
using AvalonDock;
using AvalonDock.Layout;

namespace ArtStudio.WPF.Services;

/// <summary>
/// WPF implementation of workspace layout management using AvalonDock
/// </summary>
public class WorkspaceLayoutManager : IWorkspaceLayoutManager
{
    private readonly ILogger<WorkspaceLayoutManager>? _logger;
    private DockingManager? _dockingManager;
    private readonly Dictionary<string, FrameworkElement> _availablePanels = new();
    private readonly Dictionary<string, ToolBar> _availableToolbars = new();

    // High-performance logging delegates
    private static readonly Action<ILogger, Exception?> LogInitialized =
        LoggerMessage.Define(LogLevel.Debug, new EventId(1, nameof(Initialize)),
            "Initialized workspace layout manager with docking manager");

    private static readonly Action<ILogger, string, Exception?> LogPanelRegistered =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(2, nameof(RegisterPanel)),
            "Registered panel: {PanelId}");

    private static readonly Action<ILogger, string, Exception?> LogToolbarRegistered =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(3, nameof(RegisterToolbar)),
            "Registered toolbar: {ToolbarId}");

    private static readonly Action<ILogger, string, Exception?> LogLayoutManagerNotInitialized =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(4, nameof(ApplyWorkspaceAsync)),
            "Layout manager not initialized, deferring workspace application: {WorkspaceName}");

    private static readonly Action<ILogger, string, Exception?> LogApplyingWorkspace =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(5, nameof(ApplyWorkspaceAsync)),
            "Applying workspace: {WorkspaceName}");

    private static readonly Action<ILogger, string, Exception?> LogFailedToApplyLayoutData =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(6, nameof(ApplyWorkspaceAsync)),
            "Failed to apply layout data for workspace: {WorkspaceName}");

    private static readonly Action<ILogger, string, Exception?> LogWorkspaceAppliedSuccessfully =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(7, nameof(ApplyWorkspaceAsync)),
            "Successfully applied workspace: {WorkspaceName}");

    private static readonly Action<ILogger, string, Exception?> LogFailedToApplyWorkspace =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(8, nameof(ApplyWorkspaceAsync)),
            "Failed to apply workspace: {WorkspaceName}");

    private static readonly Action<ILogger, Exception?> LogLayoutManagerNotInitializedForCapture =
        LoggerMessage.Define(LogLevel.Warning, new EventId(9, nameof(CaptureCurrentLayoutAsync)),
            "Layout manager not initialized, returning empty workspace configuration");

    private static readonly Action<ILogger, string, Exception?> LogWorkspaceCaptured =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(10, nameof(CaptureCurrentLayoutAsync)),
            "Captured current layout as workspace: {WorkspaceName}");

    private static readonly Action<ILogger, string, Exception?> LogPanelNotFoundInRegistry =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(11, nameof(AddPanelAsync)),
            "Panel not found in registry: {PanelId}");

    private static readonly Action<ILogger, string, string, Exception?> LogPanelAdded =
        LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(12, nameof(AddPanelAsync)),
            "Added panel: {PanelId} to {DockSide}");

    private static readonly Action<ILogger, string, Exception?> LogPanelRemoved =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(13, nameof(RemovePanelAsync)),
            "Removed panel: {PanelId}");

    private static readonly Action<ILogger, string, Exception?> LogPanelNotFoundForMove =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(14, nameof(MovePanelAsync)),
            "Panel not found for move: {PanelId}");

    private static readonly Action<ILogger, string, string, Exception?> LogPanelMoved =
        LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(15, nameof(MovePanelAsync)),
            "Moved panel: {PanelId} to {DockSide}");

    private static readonly Action<ILogger, string, double, double, Exception?> LogPanelResized =
        LoggerMessage.Define<string, double, double>(LogLevel.Debug, new EventId(16, nameof(ResizePanelAsync)),
            "Resized panel: {PanelId} to {Width}x{Height}");

    private static readonly Action<ILogger, string, bool, Exception?> LogPanelVisibilitySet =
        LoggerMessage.Define<string, bool>(LogLevel.Debug, new EventId(17, nameof(SetPanelVisibilityAsync)),
            "Set panel visibility: {PanelId} = {IsVisible}");

    private static readonly Action<ILogger, string, Exception?> LogToolbarAdded =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(18, nameof(AddToolbarAsync)),
            "Added toolbar: {ToolbarId}");

    private static readonly Action<ILogger, string, Exception?> LogToolbarRemoved =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(19, nameof(RemoveToolbarAsync)),
            "Removed toolbar: {ToolbarId}");

    private static readonly Action<ILogger, string, string, Exception?> LogToolbarMoved =
        LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(20, nameof(MoveToolbarAsync)),
            "Moved toolbar: {ToolbarId} to {Position}");

    private static readonly Action<ILogger, string, string, Exception?> LogCommandAddedToToolbar =
        LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(21, nameof(AddToolbarCommandAsync)),
            "Added command {CommandId} to toolbar: {ToolbarId}");

    private static readonly Action<ILogger, string, string, Exception?> LogCommandRemovedFromToolbar =
        LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(22, nameof(RemoveToolbarCommandAsync)),
            "Removed command {CommandId} from toolbar: {ToolbarId}");

    private static readonly Action<ILogger, string, Exception?> LogToolbarCommandsReordered =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(23, nameof(ReorderToolbarCommandsAsync)),
            "Reordered commands in toolbar: {ToolbarId}");

    private static readonly Action<ILogger, string, bool, Exception?> LogToolbarVisibilitySet =
        LoggerMessage.Define<string, bool>(LogLevel.Debug, new EventId(24, nameof(SetToolbarVisibilityAsync)),
            "Set toolbar visibility: {ToolbarId} = {IsVisible}");

    private static readonly Action<ILogger, Exception?> LogResetToDefaultLayout =
        LoggerMessage.Define(LogLevel.Information, new EventId(25, nameof(ResetToDefaultLayoutAsync)),
            "Reset to default layout");

    private static readonly Action<ILogger, Exception?> LogLayoutDataApplied =
        LoggerMessage.Define(LogLevel.Debug, new EventId(26, "ApplyLayoutData"),
            "Applied layout data (placeholder implementation)");

    public WorkspaceLayoutManager(ILogger<WorkspaceLayoutManager>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initialize the layout manager with the docking manager
    /// </summary>
    public void Initialize(DockingManager dockingManager)
    {
        _dockingManager = dockingManager ?? throw new ArgumentNullException(nameof(dockingManager));
        if (_logger != null)
            LogInitialized(_logger, null);
    }

    /// <summary>
    /// Register a panel for use in workspaces
    /// </summary>
    public void RegisterPanel(string panelId, FrameworkElement panelContent)
    {
        if (string.IsNullOrWhiteSpace(panelId))
            throw new ArgumentException("Panel ID cannot be empty", nameof(panelId));
        ArgumentNullException.ThrowIfNull(panelContent);

        _availablePanels[panelId] = panelContent;
        if (_logger != null)
            LogPanelRegistered(_logger, panelId, null);
    }

    /// <summary>
    /// Register a toolbar for use in workspaces
    /// </summary>
    public void RegisterToolbar(string toolbarId, ToolBar toolbar)
    {
        if (string.IsNullOrWhiteSpace(toolbarId))
            throw new ArgumentException("Toolbar ID cannot be empty", nameof(toolbarId));
        ArgumentNullException.ThrowIfNull(toolbar);

        _availableToolbars[toolbarId] = toolbar;
        if (_logger != null)
            LogToolbarRegistered(_logger, toolbarId, null);
    }

    /// <inheritdoc />
    public Task ApplyWorkspaceAsync(WorkspaceConfiguration workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        if (_dockingManager == null)
        {
            if (_logger != null)
                LogLayoutManagerNotInitialized(_logger, workspace.Name, null);
            return Task.CompletedTask;
        }

        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                if (_logger != null)
                    LogApplyingWorkspace(_logger, workspace.Name, null);

                // Clear current layout
                ClearLayout();

                // Apply panels
                ApplyPanels(workspace.Panels);

                // Apply toolbars
                ApplyToolbars(workspace.Toolbars);

                // Apply layout data if available
                if (!string.IsNullOrWhiteSpace(workspace.LayoutData))
                {
                    try
                    {
                        ApplyLayoutData(workspace.LayoutData);
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    // Gracefully handle layout data errors to allow partial workspace application
                    catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                    {
                        if (_logger != null)
                            LogFailedToApplyLayoutData(_logger, workspace.Name, ex);
                    }
                }

                if (_logger != null)
                    LogWorkspaceAppliedSuccessfully(_logger, workspace.Name, null);
            }
            catch (Exception ex)
            {
                if (_logger != null)
                    LogFailedToApplyWorkspace(_logger, workspace.Name, ex);
                throw;
            }
        }).Task;
    }

    /// <inheritdoc />
    public Task<WorkspaceConfiguration> CaptureCurrentLayoutAsync(string name, string description = "")
    {
        if (_dockingManager == null)
        {
            if (_logger != null)
                LogLayoutManagerNotInitializedForCapture(_logger, null);
            return Task.FromResult(new WorkspaceConfiguration
            {
                Name = name,
                Description = description
            });
        }

        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var workspace = new WorkspaceConfiguration
            {
                Name = name,
                Description = description
            };

            // Capture panels
            var panels = CapturePanels();
            foreach (var panel in panels)
            {
                workspace.Panels.Add(panel);
            }

            // Capture toolbars
            var toolbars = CaptureToolbars();
            foreach (var toolbar in toolbars)
            {
                workspace.Toolbars.Add(toolbar);
            }

            // Capture layout data
            workspace.LayoutData = CaptureLayoutData();

            if (_logger != null)
                LogWorkspaceCaptured(_logger, name, null);
            return workspace;
        }).Task;
    }

    /// <inheritdoc />
    public Task AddPanelAsync(PanelConfiguration panel)
    {
        if (_dockingManager == null)
            throw new InvalidOperationException("Layout manager not initialized");
        ArgumentNullException.ThrowIfNull(panel);

        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (!_availablePanels.TryGetValue(panel.Id, out var panelContent))
            {
                if (_logger != null)
                    LogPanelNotFoundInRegistry(_logger, panel.Id, null);
                return;
            }

            var anchorable = CreateAnchorable(panel, panelContent);
            var pane = GetOrCreatePane(panel.Position.DockSide, panel.Position.PaneGroup);
            pane.Children.Add(anchorable);

            if (_logger != null)
                LogPanelAdded(_logger, panel.Id, panel.Position.DockSide.ToString(), null);
        }).Task;
    }

    /// <inheritdoc />
    public Task RemovePanelAsync(string panelId)
    {
        if (_dockingManager == null)
            throw new InvalidOperationException("Layout manager not initialized");

        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var anchorable = FindAnchorable(panelId);
            if (anchorable != null)
            {
                anchorable.Close();
                if (_logger != null)
                    LogPanelRemoved(_logger, panelId, null);
            }
        }).Task;
    }

    /// <inheritdoc />
    public Task MovePanelAsync(string panelId, PanelPosition newPosition)
    {
        if (_dockingManager == null)
            throw new InvalidOperationException("Layout manager not initialized");

        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var anchorable = FindAnchorable(panelId);
            if (anchorable == null)
            {
                if (_logger != null)
                    LogPanelNotFoundForMove(_logger, panelId, null);
                return;
            }

            // Remove from current position
            var currentParent = anchorable.Parent;
            if (currentParent is LayoutAnchorablePane currentPane)
            {
                currentPane.RemoveChild(anchorable);
            }

            // Add to new position
            var newPane = GetOrCreatePane(newPosition.DockSide, newPosition.PaneGroup);
            newPane.Children.Insert(Math.Min(newPosition.Order, newPane.Children.Count), anchorable);

            if (_logger != null)
                LogPanelMoved(_logger, panelId, newPosition.DockSide.ToString(), null);
        }).Task;
    }

    /// <inheritdoc />
    public Task ResizePanelAsync(string panelId, PanelSize newSize)
    {
        if (_dockingManager == null)
            throw new InvalidOperationException("Layout manager not initialized");

        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var anchorable = FindAnchorable(panelId);
            if (anchorable?.Parent is LayoutAnchorablePane pane)
            {
                if (pane.DockWidth.Value != newSize.Width)
                    pane.DockWidth = new GridLength(newSize.Width);
                if (pane.DockHeight.Value != newSize.Height)
                    pane.DockHeight = new GridLength(newSize.Height);

                if (_logger != null)
                    LogPanelResized(_logger, panelId, newSize.Width, newSize.Height, null);
            }
        }).Task;
    }

    /// <inheritdoc />
    public Task SetPanelVisibilityAsync(string panelId, bool isVisible)
    {
        if (_dockingManager == null)
            throw new InvalidOperationException("Layout manager not initialized");

        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var anchorable = FindAnchorable(panelId);
            if (anchorable != null)
            {
                if (isVisible && !anchorable.IsVisible)
                {
                    anchorable.Show();
                }
                else if (!isVisible && anchorable.IsVisible)
                {
                    anchorable.Hide();
                }

                if (_logger != null)
                    LogPanelVisibilitySet(_logger, panelId, isVisible, null);
            }
        }).Task;
    }

    /// <inheritdoc />
    public Task AddToolbarAsync(ToolbarConfiguration toolbar)
    {
        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            // Implementation would depend on how toolbars are managed in the main window
            // This is a placeholder for the actual toolbar management logic
            if (_logger != null)
                LogToolbarAdded(_logger, toolbar.Id, null);
        }).Task;
    }

    /// <inheritdoc />
    public Task RemoveToolbarAsync(string toolbarId)
    {
        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            // Placeholder for toolbar removal logic
            if (_logger != null)
                LogToolbarRemoved(_logger, toolbarId, null);
        }).Task;
    }

    /// <inheritdoc />
    public Task MoveToolbarAsync(string toolbarId, ToolbarPosition newPosition, int order)
    {
        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            // Placeholder for toolbar move logic
            if (_logger != null)
                LogToolbarMoved(_logger, toolbarId, newPosition.ToString(), null);
        }).Task;
    }

    /// <inheritdoc />
    public Task AddToolbarCommandAsync(string toolbarId, ToolbarItem item)
    {
        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            // Placeholder for adding command to toolbar
            if (_logger != null)
                LogCommandAddedToToolbar(_logger, item.CommandId ?? string.Empty, toolbarId, null);
        }).Task;
    }

    /// <inheritdoc />
    public Task RemoveToolbarCommandAsync(string toolbarId, string commandId)
    {
        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            // Placeholder for removing command from toolbar
            if (_logger != null)
                LogCommandRemovedFromToolbar(_logger, commandId, toolbarId, null);
        }).Task;
    }

    /// <inheritdoc />
    public Task ReorderToolbarCommandsAsync(string toolbarId, IList<string> commandOrder)
    {
        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            // Placeholder for reordering toolbar commands
            if (_logger != null)
                LogToolbarCommandsReordered(_logger, toolbarId, null);
        }).Task;
    }

    /// <inheritdoc />
    public Task SetToolbarVisibilityAsync(string toolbarId, bool isVisible)
    {
        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (_availableToolbars.TryGetValue(toolbarId, out var toolbar))
            {
                toolbar.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
                if (_logger != null)
                    LogToolbarVisibilitySet(_logger, toolbarId, isVisible, null);
            }
        }).Task;
    }

    /// <inheritdoc />
    public Task ResetToDefaultLayoutAsync()
    {
        if (_dockingManager == null)
            throw new InvalidOperationException("Layout manager not initialized");

        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            ClearLayout();
            CreateDefaultLayout();
            if (_logger != null)
                LogResetToDefaultLayout(_logger, null);
        }).Task;
    }

    private void ClearLayout()
    {
        if (_dockingManager?.Layout?.RootPanel != null)
        {
            _dockingManager.Layout.RootPanel.Children.Clear();
        }
    }

    private void ApplyPanels(Collection<PanelConfiguration> panels)
    {
        foreach (var panel in panels.Where(p => p.IsVisible).OrderBy(p => p.Position.Order))
        {
            if (_availablePanels.TryGetValue(panel.Id, out var panelContent))
            {
                var anchorable = CreateAnchorable(panel, panelContent);
                var pane = GetOrCreatePane(panel.Position.DockSide, panel.Position.PaneGroup);
                pane.Children.Add(anchorable);
            }
            else
            {
                if (_logger != null)
                    LogPanelNotFoundInRegistry(_logger, panel.Id, null);
            }
        }
    }

    private void ApplyToolbars(Collection<ToolbarConfiguration> toolbars)
    {
        foreach (var toolbar in toolbars.Where(t => t.IsVisible))
        {
            if (_availableToolbars.TryGetValue(toolbar.Id, out var toolbarElement))
            {
                toolbarElement.Visibility = Visibility.Visible;
                // Additional toolbar configuration would go here
            }
        }
    }

    private void ApplyLayoutData(string layoutData)
    {
        // In a real implementation, this would deserialize and apply
        // AvalonDock-specific layout data
        if (_logger != null)
            LogLayoutDataApplied(_logger, null);
    }

    private List<PanelConfiguration> CapturePanels()
    {
        var panels = new List<PanelConfiguration>();

        if (_dockingManager?.Layout?.RootPanel != null)
        {
            CapturePanelsRecursive(_dockingManager.Layout.RootPanel, panels);
        }

        return panels;
    }

    private void CapturePanelsRecursive(ILayoutElement element, List<PanelConfiguration> panels)
    {
        switch (element)
        {
            case LayoutAnchorable anchorable:
                var panel = CreatePanelConfiguration(anchorable);
                if (panel != null)
                    panels.Add(panel);
                break;

            case ILayoutContainer container:
                foreach (var child in container.Children)
                {
                    CapturePanelsRecursive(child, panels);
                }
                break;
        }
    }

    private PanelConfiguration? CreatePanelConfiguration(LayoutAnchorable anchorable)
    {
        if (string.IsNullOrWhiteSpace(anchorable.ContentId))
            return null;

        var dockSide = DetermineDockSide(anchorable);
        var order = DetermineOrder(anchorable);
        var parentPane = anchorable.Parent as LayoutAnchorablePane;

        return new PanelConfiguration
        {
            Id = anchorable.ContentId,
            Name = anchorable.Title ?? anchorable.ContentId,
            Type = DeterminePanelType(anchorable.ContentId),
            IsVisible = anchorable.IsVisible,
            Position = new PanelPosition
            {
                DockSide = dockSide,
                IsFloating = anchorable.IsFloating,
                Order = order
            },
            Size = new PanelSize
            {
                Width = parentPane?.DockWidth.Value ?? 200,
                Height = parentPane?.DockHeight.Value ?? 200
            },
            CanClose = anchorable.CanClose,
            CanHide = anchorable.CanHide,
            CanFloat = anchorable.CanFloat
        };
    }

    private List<ToolbarConfiguration> CaptureToolbars()
    {
        var toolbars = new List<ToolbarConfiguration>();

        foreach (var (toolbarId, toolbar) in _availableToolbars)
        {
            var toolbarConfig = new ToolbarConfiguration
            {
                Id = toolbarId,
                Name = toolbarId,
                IsVisible = toolbar.Visibility == Visibility.Visible,
                Position = ToolbarPosition.Top, // Simplified for now
                Order = 0
            };

            var items = CaptureToolbarItems(toolbar);
            foreach (var item in items)
            {
                toolbarConfig.Items.Add(item);
            }

            toolbars.Add(toolbarConfig);
        }

        return toolbars;
    }

    private List<ToolbarItem> CaptureToolbarItems(ToolBar toolbar)
    {
        var items = new List<ToolbarItem>();
        var order = 0;

        foreach (var item in toolbar.Items)
        {
            switch (item)
            {
                case Button button:
                    items.Add(new ToolbarItem
                    {
                        Type = ToolbarItemType.Command,
                        Text = button.ToolTip?.ToString(),
                        ToolTip = button.ToolTip?.ToString(),
                        IsVisible = button.Visibility == Visibility.Visible,
                        IsEnabled = button.IsEnabled,
                        Order = order++
                    });
                    break;

                case Separator:
                    items.Add(new ToolbarItem
                    {
                        Type = ToolbarItemType.Separator,
                        Order = order++
                    });
                    break;
            }
        }

        return items;
    }

    private string CaptureLayoutData()
    {
        // In a real implementation, this would serialize the current
        // AvalonDock layout for precise restoration
        return string.Empty;
    }

    private LayoutAnchorable CreateAnchorable(PanelConfiguration panel, FrameworkElement content)
    {
        return new LayoutAnchorable
        {
            ContentId = panel.Id,
            Title = panel.Name,
            Content = content,
            CanClose = panel.CanClose,
            CanHide = panel.CanHide,
            CanFloat = panel.CanFloat
        };
    }

    private LayoutAnchorablePane GetOrCreatePane(DockSide dockSide, string? paneGroup)
    {
        // Find existing pane or create new one
        var rootPanel = _dockingManager?.Layout?.RootPanel;
        if (rootPanel == null)
        {
            CreateDefaultLayout();
            rootPanel = _dockingManager?.Layout?.RootPanel;
        }

        // Simplified implementation - in practice, this would be more sophisticated
        var pane = new LayoutAnchorablePane();

        switch (dockSide)
        {
            case DockSide.Left:
                if (rootPanel!.Children.Count == 0)
                    rootPanel.Children.Add(new LayoutPanel { Orientation = Orientation.Horizontal });
                var horizontalPanel = rootPanel.Children[0] as LayoutPanel;
                horizontalPanel!.Children.Insert(0, pane);
                break;

            case DockSide.Right:
                if (rootPanel!.Children.Count == 0)
                    rootPanel.Children.Add(new LayoutPanel { Orientation = Orientation.Horizontal });
                horizontalPanel = rootPanel.Children[0] as LayoutPanel;
                horizontalPanel!.Children.Add(pane);
                break;

            case DockSide.Top:
                if (rootPanel!.Children.Count == 0)
                    rootPanel.Children.Add(new LayoutPanel { Orientation = Orientation.Vertical });
                var verticalPanel = rootPanel.Children[0] as LayoutPanel;
                verticalPanel!.Children.Insert(0, pane);
                break;

            case DockSide.Bottom:
                if (rootPanel!.Children.Count == 0)
                    rootPanel.Children.Add(new LayoutPanel { Orientation = Orientation.Vertical });
                verticalPanel = rootPanel.Children[0] as LayoutPanel;
                verticalPanel!.Children.Add(pane);
                break;

            default:
                if (rootPanel!.Children.Count == 0)
                    rootPanel.Children.Add(new LayoutPanel());
                (rootPanel.Children[0] as LayoutPanel)!.Children.Add(pane);
                break;
        }

        return pane;
    }

    private LayoutAnchorable? FindAnchorable(string contentId)
    {
        return FindAnchorableRecursive(_dockingManager?.Layout?.RootPanel, contentId);
    }

    private LayoutAnchorable? FindAnchorableRecursive(ILayoutElement? element, string contentId)
    {
        switch (element)
        {
            case LayoutAnchorable anchorable when anchorable.ContentId == contentId:
                return anchorable;

            case ILayoutContainer container:
                foreach (var child in container.Children)
                {
                    var result = FindAnchorableRecursive(child, contentId);
                    if (result != null)
                        return result;
                }
                break;
        }

        return null;
    }

    private void CreateDefaultLayout()
    {
        if (_dockingManager?.Layout != null)
        {
            var rootPanel = new LayoutPanel { Orientation = Orientation.Horizontal };
            var documentPane = new LayoutDocumentPane();

            rootPanel.Children.Add(documentPane);
            _dockingManager.Layout.RootPanel = rootPanel;
        }
    }

    private DockSide DetermineDockSide(LayoutAnchorable anchorable)
    {
        // Simplified logic to determine dock side based on position
        // In practice, this would analyze the layout tree structure
        return DockSide.Left;
    }

    private int DetermineOrder(LayoutAnchorable anchorable)
    {
        // Determine order within the pane
        if (anchorable.Parent is LayoutAnchorablePane pane)
        {
            return pane.Children.IndexOf(anchorable);
        }
        return 0;
    }

    private PanelType DeterminePanelType(string panelId)
    {
        return panelId.ToUpperInvariant() switch
        {
            "TOOLPALETTE" => PanelType.ToolPalette,
            "LAYERPANEL" => PanelType.LayerPanel,
            "PROPERTIES" => PanelType.Properties,
            "COLORPICKER" => PanelType.ColorPicker,
            "BRUSHSETTINGS" => PanelType.BrushSettings,
            "HISTORY" => PanelType.History,
            "NAVIGATOR" => PanelType.Navigator,
            _ => PanelType.Custom
        };
    }
}
