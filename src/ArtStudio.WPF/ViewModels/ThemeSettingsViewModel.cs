using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ArtStudio.Core.Interfaces;

namespace ArtStudio.WPF.ViewModels;

/// <summary>
/// ViewModel for theme settings and configuration
/// </summary>
public class ThemeSettingsViewModel : INotifyPropertyChanged
{
    private readonly IConfigurationManager _configurationManager;
    private readonly IThemeManager _themeManager;
    private string _selectedTheme;
    private string _selectedMaterialDesignBaseTheme;
    private string _selectedPrimaryColor;
    private string _selectedSecondaryColor;
    private bool _useSystemTheme;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> AvailableThemes { get; }
    public ObservableCollection<string> MaterialDesignBaseThemes { get; }
    public ObservableCollection<string> PrimaryColors { get; }
    public ObservableCollection<string> SecondaryColors { get; }

    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (SetProperty(ref _selectedTheme, value))
            {
                _configurationManager.CurrentTheme = value;
                if (!UseSystemTheme)
                {
                    _themeManager.ApplyTheme(value);
                }
            }
        }
    }

    public string SelectedMaterialDesignBaseTheme
    {
        get => _selectedMaterialDesignBaseTheme;
        set
        {
            if (SetProperty(ref _selectedMaterialDesignBaseTheme, value))
            {
                _configurationManager.MaterialDesignBaseTheme = value;
                RefreshTheme();
            }
        }
    }

    public string SelectedPrimaryColor
    {
        get => _selectedPrimaryColor;
        set
        {
            if (SetProperty(ref _selectedPrimaryColor, value))
            {
                _configurationManager.MaterialDesignPrimaryColor = value;
                RefreshTheme();
            }
        }
    }

    public string SelectedSecondaryColor
    {
        get => _selectedSecondaryColor;
        set
        {
            if (SetProperty(ref _selectedSecondaryColor, value))
            {
                _configurationManager.MaterialDesignSecondaryColor = value;
                RefreshTheme();
            }
        }
    }

    public bool UseSystemTheme
    {
        get => _useSystemTheme;
        set
        {
            if (SetProperty(ref _useSystemTheme, value))
            {
                _configurationManager.UseSystemTheme = value;
                if (value)
                {
                    _themeManager.ApplySystemTheme();
                }
            }
        }
    }

    public ThemeSettingsViewModel(IConfigurationManager configurationManager, IThemeManager themeManager)
    {
        _configurationManager = configurationManager;
        _themeManager = themeManager;

        AvailableThemes = new ObservableCollection<string>(_themeManager.AvailableThemes);
        MaterialDesignBaseThemes = new ObservableCollection<string> { "Light", "Dark" };
        PrimaryColors = new ObservableCollection<string>
        {
            "Red", "Pink", "Purple", "DeepPurple", "Indigo", "Blue", "LightBlue",
            "Cyan", "Teal", "Green", "LightGreen", "Lime", "Yellow", "Amber",
            "Orange", "DeepOrange", "Brown", "Grey", "BlueGrey"
        };
        SecondaryColors = new ObservableCollection<string>
        {
            "Red", "Pink", "Purple", "DeepPurple", "Indigo", "Blue", "LightBlue",
            "Cyan", "Teal", "Green", "LightGreen", "Lime", "Yellow", "Amber",
            "Orange", "DeepOrange", "Brown", "Grey", "BlueGrey"
        };

        // Load current values
        _selectedTheme = _configurationManager.CurrentTheme;
        _selectedMaterialDesignBaseTheme = _configurationManager.MaterialDesignBaseTheme;
        _selectedPrimaryColor = _configurationManager.MaterialDesignPrimaryColor;
        _selectedSecondaryColor = _configurationManager.MaterialDesignSecondaryColor;
        _useSystemTheme = _configurationManager.UseSystemTheme;

        // Subscribe to configuration changes
        _configurationManager.ConfigurationChanged += OnConfigurationChanged;
    }

    private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        switch (e.Key)
        {
            case "CurrentTheme":
                if (e.NewValue is string theme)
                    _selectedTheme = theme;
                OnPropertyChanged(nameof(SelectedTheme));
                break;
            case "MaterialDesignBaseTheme":
                if (e.NewValue is string baseTheme)
                    _selectedMaterialDesignBaseTheme = baseTheme;
                OnPropertyChanged(nameof(SelectedMaterialDesignBaseTheme));
                break;
            case "MaterialDesignPrimaryColor":
                if (e.NewValue is string primaryColor)
                    _selectedPrimaryColor = primaryColor;
                OnPropertyChanged(nameof(SelectedPrimaryColor));
                break;
            case "MaterialDesignSecondaryColor":
                if (e.NewValue is string secondaryColor)
                    _selectedSecondaryColor = secondaryColor;
                OnPropertyChanged(nameof(SelectedSecondaryColor));
                break;
            case "UseSystemTheme":
                if (e.NewValue is bool useSystemTheme)
                    _useSystemTheme = useSystemTheme;
                OnPropertyChanged(nameof(UseSystemTheme));
                break;
        }
    }

    private void RefreshTheme()
    {
        if (!UseSystemTheme)
        {
            _themeManager.ApplyTheme(SelectedTheme);
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
