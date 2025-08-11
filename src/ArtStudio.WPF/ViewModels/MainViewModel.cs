using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ArtStudio.Core;
using Microsoft.Extensions.Logging;

namespace ArtStudio.WPF.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IWorkspaceManager _workspaceManager;
    private readonly IEditorService _editorService;
    private readonly ILogger<MainViewModel>? _logger;
    private readonly IConfigurationManager? _configurationManager;
    private readonly IThemeManager? _themeManager;

    private string _statusText = "Ready";
    private int _zoomLevel = 100;
    private bool _isToolPaletteVisible = true;
    private bool _isLayerPaletteVisible = true;
    private bool _isPropertiesVisible = true;
    private bool _isWorkspaceVisible = false;

    public MainViewModel(
        IWorkspaceManager workspaceManager,
        IEditorService editorService,
        ILogger<MainViewModel>? logger,
        ToolPaletteViewModel toolPaletteViewModel,
        LayerPaletteViewModel layerPaletteViewModel,
        EditorViewModel editorViewModel,
        IConfigurationManager? configurationManager = null,
        IThemeManager? themeManager = null,
        WorkspaceViewModel? workspaceViewModel = null)
    {
        _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
        _editorService = editorService;
        _logger = logger;
        _configurationManager = configurationManager;
        _themeManager = themeManager;

        ToolPaletteViewModel = toolPaletteViewModel;
        LayerPaletteViewModel = layerPaletteViewModel;
        EditorViewModel = editorViewModel;
        WorkspaceViewModel = workspaceViewModel;

        // Create theme settings view model if managers are available
        if (_configurationManager != null && _themeManager != null)
        {
            ThemeSettingsViewModel = new ThemeSettingsViewModel(_configurationManager, _themeManager);
        }

        InitializeCommands();
    }

    public ToolPaletteViewModel ToolPaletteViewModel { get; }
    public LayerPaletteViewModel LayerPaletteViewModel { get; }
    public EditorViewModel EditorViewModel { get; }
    public ThemeSettingsViewModel? ThemeSettingsViewModel { get; }
    public WorkspaceViewModel? WorkspaceViewModel { get; }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public int ZoomLevel
    {
        get => _zoomLevel;
        set => SetProperty(ref _zoomLevel, value);
    }

    public bool IsToolPaletteVisible
    {
        get => _isToolPaletteVisible;
        set => SetProperty(ref _isToolPaletteVisible, value);
    }

    public bool IsLayerPaletteVisible
    {
        get => _isLayerPaletteVisible;
        set => SetProperty(ref _isLayerPaletteVisible, value);
    }

    public bool IsPropertiesVisible
    {
        get => _isPropertiesVisible;
        set => SetProperty(ref _isPropertiesVisible, value);
    }

    public bool IsWorkspaceVisible
    {
        get => _isWorkspaceVisible;
        set => SetProperty(ref _isWorkspaceVisible, value);
    }

    // Commands
    public ICommand NewCommand { get; private set; } = null!;
    public ICommand OpenCommand { get; private set; } = null!;
    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand SaveAsCommand { get; private set; } = null!;
    public ICommand ExitCommand { get; private set; } = null!;
    public ICommand UndoCommand { get; private set; } = null!;
    public ICommand RedoCommand { get; private set; } = null!;
    public ICommand CutCommand { get; private set; } = null!;
    public ICommand CopyCommand { get; private set; } = null!;
    public ICommand PasteCommand { get; private set; } = null!;
    public ICommand AboutCommand { get; private set; } = null!;
    public ICommand SelectBrushCommand { get; private set; } = null!;
    public ICommand SelectEraserCommand { get; private set; } = null!;
    public ICommand SelectSelectionCommand { get; private set; } = null!;
    public ICommand ToggleWorkspaceCommand { get; private set; } = null!;

    // New commands for enhanced menu
    public ICommand NewCanvasCommand { get; private set; } = null!;
    public ICommand ExportCommand { get; private set; } = null!;
    public ICommand ImportCommand { get; private set; } = null!;
    public ICommand ClearCommand { get; private set; } = null!;
    public ICommand SelectAllCommand { get; private set; } = null!;
    public ICommand ToggleToolPaletteCommand { get; private set; } = null!;
    public ICommand ToggleLayerPaletteCommand { get; private set; } = null!;
    public ICommand TogglePropertiesPanelCommand { get; private set; } = null!;
    public ICommand ZoomInCommand { get; private set; } = null!;
    public ICommand ZoomOutCommand { get; private set; } = null!;
    public ICommand ZoomToFitCommand { get; private set; } = null!;
    public ICommand ActualSizeCommand { get; private set; } = null!;
    public ICommand ToggleFullscreenCommand { get; private set; } = null!;
    public ICommand SelectBrushToolCommand { get; private set; } = null!;
    public ICommand SelectEraserToolCommand { get; private set; } = null!;
    public ICommand SelectSelectionToolCommand { get; private set; } = null!;
    public ICommand SelectTextToolCommand { get; private set; } = null!;
    public ICommand OpenPreferencesCommand { get; private set; } = null!;

    // Workspace management commands
    public ICommand SwitchToDrawingWorkspaceCommand { get; private set; } = null!;
    public ICommand SwitchToPhotoEditingWorkspaceCommand { get; private set; } = null!;
    public ICommand SwitchToCompositingWorkspaceCommand { get; private set; } = null!;
    public ICommand ManageWorkspacesCommand { get; private set; } = null!;
    public ICommand CreateNewWorkspaceCommand { get; private set; } = null!;
    public ICommand ResetCurrentWorkspaceCommand { get; private set; } = null!;

    // Help commands
    public ICommand OpenUserGuideCommand { get; private set; } = null!;
    public ICommand ShowKeyboardShortcutsCommand { get; private set; } = null!;
    public ICommand ReportIssueCommand { get; private set; } = null!;
    public ICommand CheckForUpdatesCommand { get; private set; } = null!;

    // Additional properties
    private bool _isFullscreen = false;
    public bool IsFullscreen
    {
        get => _isFullscreen;
        set => SetProperty(ref _isFullscreen, value);
    }

    public bool IsPropertiesPanelVisible
    {
        get => _isPropertiesVisible;
        set => SetProperty(ref _isPropertiesVisible, value);
    }

    private void InitializeCommands()
    {
        // Existing commands
        NewCommand = new RelayCommand(ExecuteNew);
        OpenCommand = new RelayCommand(ExecuteOpen);
        SaveCommand = new RelayCommand(ExecuteSave);
        SaveAsCommand = new RelayCommand(ExecuteSaveAs);
        ExitCommand = new RelayCommand(ExecuteExit);
        UndoCommand = new RelayCommand(ExecuteUndo);
        RedoCommand = new RelayCommand(ExecuteRedo);
        CutCommand = new RelayCommand(ExecuteCut);
        CopyCommand = new RelayCommand(ExecuteCopy);
        PasteCommand = new RelayCommand(ExecutePaste);
        AboutCommand = new RelayCommand(ExecuteAbout);
        SelectBrushCommand = new RelayCommand(ExecuteSelectBrush);
        SelectEraserCommand = new RelayCommand(ExecuteSelectEraser);
        SelectSelectionCommand = new RelayCommand(ExecuteSelectSelection);
        ToggleWorkspaceCommand = new RelayCommand(ExecuteToggleWorkspace);

        // New enhanced commands
        NewCanvasCommand = new RelayCommand(ExecuteNewCanvas);
        ExportCommand = new RelayCommand(ExecuteExport);
        ImportCommand = new RelayCommand(ExecuteImport);
        ClearCommand = new RelayCommand(ExecuteClear);
        SelectAllCommand = new RelayCommand(ExecuteSelectAll);
        ToggleToolPaletteCommand = new RelayCommand(ExecuteToggleToolPalette);
        ToggleLayerPaletteCommand = new RelayCommand(ExecuteToggleLayerPalette);
        TogglePropertiesPanelCommand = new RelayCommand(ExecuteTogglePropertiesPanel);
        ZoomInCommand = new RelayCommand(ExecuteZoomIn);
        ZoomOutCommand = new RelayCommand(ExecuteZoomOut);
        ZoomToFitCommand = new RelayCommand(ExecuteZoomToFit);
        ActualSizeCommand = new RelayCommand(ExecuteActualSize);
        ToggleFullscreenCommand = new RelayCommand(ExecuteToggleFullscreen);
        SelectBrushToolCommand = new RelayCommand(ExecuteSelectBrushTool);
        SelectEraserToolCommand = new RelayCommand(ExecuteSelectEraserTool);
        SelectSelectionToolCommand = new RelayCommand(ExecuteSelectSelectionTool);
        SelectTextToolCommand = new RelayCommand(ExecuteSelectTextTool);
        OpenPreferencesCommand = new RelayCommand(ExecuteOpenPreferences);

        // Workspace management commands
        SwitchToDrawingWorkspaceCommand = new RelayCommand(ExecuteSwitchToDrawingWorkspace);
        SwitchToPhotoEditingWorkspaceCommand = new RelayCommand(ExecuteSwitchToPhotoEditingWorkspace);
        SwitchToCompositingWorkspaceCommand = new RelayCommand(ExecuteSwitchToCompositingWorkspace);
        ManageWorkspacesCommand = new RelayCommand(ExecuteManageWorkspaces);
        CreateNewWorkspaceCommand = new RelayCommand(ExecuteCreateNewWorkspace);
        ResetCurrentWorkspaceCommand = new RelayCommand(ExecuteResetCurrentWorkspace);

        // Help commands
        OpenUserGuideCommand = new RelayCommand(ExecuteOpenUserGuide);
        ShowKeyboardShortcutsCommand = new RelayCommand(ExecuteShowKeyboardShortcuts);
        ReportIssueCommand = new RelayCommand(ExecuteReportIssue);
        CheckForUpdatesCommand = new RelayCommand(ExecuteCheckForUpdates);
    }

    private void ExecuteNew()
    {
        _logger?.LogInformation("Creating new document");
        StatusText = "New document created";
    }

    private void ExecuteOpen()
    {
        _logger?.LogInformation("Opening document");
        StatusText = "Opening document...";
    }

    private void ExecuteSave()
    {
        _logger?.LogInformation("Saving document");
        StatusText = "Document saved";
    }

    private void ExecuteSaveAs()
    {
        _logger?.LogInformation("Save As...");
        StatusText = "Save As...";
    }

    private void ExecuteExit()
    {
        System.Windows.Application.Current.Shutdown();
    }

    private void ExecuteUndo()
    {
        _logger?.LogInformation("Undo");
        StatusText = "Undo";
    }

    private void ExecuteRedo()
    {
        _logger?.LogInformation("Redo");
        StatusText = "Redo";
    }

    private void ExecuteCut()
    {
        StatusText = "Cut";
    }

    private void ExecuteCopy()
    {
        StatusText = "Copy";
    }

    private void ExecutePaste()
    {
        StatusText = "Paste";
    }

    private void ExecuteAbout()
    {
        StatusText = "About ArtStudio";
    }

    private void ExecuteSelectBrush()
    {
        StatusText = "Brush tool selected";
    }

    private void ExecuteSelectEraser()
    {
        StatusText = "Eraser tool selected";
    }

    private void ExecuteSelectSelection()
    {
        StatusText = "Selection tool selected";
    }

    private void ExecuteToggleWorkspace()
    {
        IsWorkspaceVisible = !IsWorkspaceVisible;
        StatusText = IsWorkspaceVisible ? "Workspace panel opened" : "Workspace panel closed";
    }

    // New enhanced command implementations
    private void ExecuteNewCanvas()
    {
        _logger?.LogInformation("Creating new canvas");
        StatusText = "New canvas created";
    }

    private void ExecuteExport()
    {
        _logger?.LogInformation("Exporting image");
        StatusText = "Exporting image...";
    }

    private void ExecuteImport()
    {
        _logger?.LogInformation("Importing image");
        StatusText = "Importing image...";
    }

    private void ExecuteClear()
    {
        _logger?.LogInformation("Clearing selection");
        StatusText = "Selection cleared";
    }

    private void ExecuteSelectAll()
    {
        _logger?.LogInformation("Selecting all");
        StatusText = "All selected";
    }

    private void ExecuteToggleToolPalette()
    {
        IsToolPaletteVisible = !IsToolPaletteVisible;
        StatusText = IsToolPaletteVisible ? "Tool palette visible" : "Tool palette hidden";
    }

    private void ExecuteToggleLayerPalette()
    {
        IsLayerPaletteVisible = !IsLayerPaletteVisible;
        StatusText = IsLayerPaletteVisible ? "Layer palette visible" : "Layer palette hidden";
    }

    private void ExecuteTogglePropertiesPanel()
    {
        IsPropertiesPanelVisible = !IsPropertiesPanelVisible;
        StatusText = IsPropertiesPanelVisible ? "Properties panel visible" : "Properties panel hidden";
    }

    private void ExecuteZoomIn()
    {
        ZoomLevel = Math.Min(ZoomLevel + 25, 800);
        StatusText = $"Zoom: {ZoomLevel}%";
    }

    private void ExecuteZoomOut()
    {
        ZoomLevel = Math.Max(ZoomLevel - 25, 10);
        StatusText = $"Zoom: {ZoomLevel}%";
    }

    private void ExecuteZoomToFit()
    {
        ZoomLevel = 100; // This would normally calculate fit-to-window zoom
        StatusText = "Zoom to fit";
    }

    private void ExecuteActualSize()
    {
        ZoomLevel = 100;
        StatusText = "Actual size (100%)";
    }

    private void ExecuteToggleFullscreen()
    {
        IsFullscreen = !IsFullscreen;
        StatusText = IsFullscreen ? "Entered fullscreen" : "Exited fullscreen";
    }

    private void ExecuteSelectBrushTool()
    {
        StatusText = "Brush tool selected";
    }

    private void ExecuteSelectEraserTool()
    {
        StatusText = "Eraser tool selected";
    }

    private void ExecuteSelectSelectionTool()
    {
        StatusText = "Selection tool selected";
    }

    private void ExecuteSelectTextTool()
    {
        StatusText = "Text tool selected";
    }

    private void ExecuteOpenPreferences()
    {
        _logger?.LogInformation("Opening preferences");
        StatusText = "Opening preferences...";
    }

    // Workspace management command implementations
    private async void ExecuteSwitchToDrawingWorkspace()
    {
        try
        {
            await _workspaceManager.SwitchToWorkspaceAsync("13bf9883-32d5-4402-93fb-187cafa30c52");
            StatusText = "Switched to Drawing workspace";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to switch to Drawing workspace");
            StatusText = "Failed to switch workspace";
        }
    }

    private async void ExecuteSwitchToPhotoEditingWorkspace()
    {
        try
        {
            await _workspaceManager.SwitchToWorkspaceAsync("a9244d44-4774-4b5c-b653-272f41285c56");
            StatusText = "Switched to Photo Editing workspace";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to switch to Photo Editing workspace");
            StatusText = "Failed to switch workspace";
        }
    }

    private async void ExecuteSwitchToCompositingWorkspace()
    {
        try
        {
            await _workspaceManager.SwitchToWorkspaceAsync("0e08c284-4f23-4736-810b-474a883760ea");
            StatusText = "Switched to Compositing workspace";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to switch to Compositing workspace");
            StatusText = "Failed to switch workspace";
        }
    }

    private void ExecuteManageWorkspaces()
    {
        _logger?.LogInformation("Opening workspace manager");
        StatusText = "Opening workspace manager...";
        // This would open a workspace management dialog
    }

    private void ExecuteCreateNewWorkspace()
    {
        _logger?.LogInformation("Creating new workspace");
        StatusText = "Creating new workspace...";
        // This would open a new workspace creation dialog
    }

    private void ExecuteResetCurrentWorkspace()
    {
        _logger?.LogInformation("Resetting current workspace");
        StatusText = "Current workspace reset";
        // This would reset the current workspace to its default layout
    }

    // Help command implementations
    private void ExecuteOpenUserGuide()
    {
        _logger?.LogInformation("Opening user guide");
        StatusText = "Opening user guide...";
        // This would open the user guide in a browser or help viewer
    }

    private void ExecuteShowKeyboardShortcuts()
    {
        _logger?.LogInformation("Showing keyboard shortcuts");
        StatusText = "Showing keyboard shortcuts...";
        // This would open a keyboard shortcuts dialog
    }

    private void ExecuteReportIssue()
    {
        _logger?.LogInformation("Opening issue reporter");
        StatusText = "Opening issue reporter...";
        // This would open a bug report form or redirect to GitHub issues
    }

    private void ExecuteCheckForUpdates()
    {
        _logger?.LogInformation("Checking for updates");
        StatusText = "Checking for updates...";
        // This would check for application updates
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke() ?? true;
    }

    public void Execute(object? parameter)
    {
        _execute();
    }
}
