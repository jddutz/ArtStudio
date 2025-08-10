using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ArtStudio.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ArtStudio.WPF.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ILayoutManager _layoutManager;
    private readonly IEditorService _editorService;
    private readonly ILogger<MainViewModel>? _logger;
    private readonly IConfigurationManager? _configurationManager;
    private readonly IThemeManager? _themeManager;

    private string _statusText = "Ready";
    private int _zoomLevel = 100;
    private bool _isToolPaletteVisible = true;
    private bool _isLayerPaletteVisible = true;
    private bool _isPropertiesVisible = true;

    public MainViewModel(
        ILayoutManager layoutManager,
        IEditorService editorService,
        ILogger<MainViewModel>? logger,
        ToolPaletteViewModel toolPaletteViewModel,
        LayerPaletteViewModel layerPaletteViewModel,
        EditorViewModel editorViewModel,
        IConfigurationManager? configurationManager = null,
        IThemeManager? themeManager = null)
    {
        _layoutManager = layoutManager;
        _editorService = editorService;
        _logger = logger;
        _configurationManager = configurationManager;
        _themeManager = themeManager;

        ToolPaletteViewModel = toolPaletteViewModel;
        LayerPaletteViewModel = layerPaletteViewModel;
        EditorViewModel = editorViewModel;

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

    private void InitializeCommands()
    {
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
