using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;

namespace ArtStudio.WPF.ViewModels;

public class ToolPaletteViewModel : INotifyPropertyChanged
{
    private string _selectedTool = "Brush";

    public string SelectedTool
    {
        get => _selectedTool;
        set => SetProperty(ref _selectedTool, value);
    }

    public ObservableCollection<string> AvailableTools { get; } = new()
    {
        "Brush",
        "Eraser",
        "Selection",
        "Line",
        "Rectangle",
        "Ellipse",
        "Text"
    };

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
