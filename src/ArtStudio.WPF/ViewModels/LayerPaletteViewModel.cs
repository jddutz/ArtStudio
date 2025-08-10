using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;

namespace ArtStudio.WPF.ViewModels;

public class LayerPaletteViewModel : INotifyPropertyChanged
{
    private LayerItem? _selectedLayer;

    public LayerItem? SelectedLayer
    {
        get => _selectedLayer;
        set => SetProperty(ref _selectedLayer, value);
    }

    public ObservableCollection<LayerItem> Layers { get; } = new()
    {
        new LayerItem { Name = "Background", IsVisible = true, Opacity = 1.0 },
        new LayerItem { Name = "Layer 1", IsVisible = true, Opacity = 1.0 }
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

public class LayerItem : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private bool _isVisible = true;
    private double _opacity = 1.0;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public double Opacity
    {
        get => _opacity;
        set => SetProperty(ref _opacity, value);
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
