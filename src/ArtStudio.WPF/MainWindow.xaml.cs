using System;
using System.ComponentModel;
using System.Windows;
using ArtStudio.WPF.ViewModels;
using ArtStudio.WPF.Services;
using ArtStudio.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ArtStudio.WPF;

public partial class MainWindow : Window
{
    private MainViewModel? _viewModel;
    private readonly IWorkspaceLayoutManager? _layoutManager;

    public MainWindow(MainViewModel viewModel, IWorkspaceLayoutManager? layoutManager = null)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _layoutManager = layoutManager;

        // Subscribe to property changes to handle workspace visibility
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Set initial visibility
        UpdateWorkspaceVisibility();

        // Initialize layout manager once the window is loaded
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Initialize the layout manager with the docking manager
        if (_layoutManager is WorkspaceLayoutManager concreteLayoutManager)
        {
            var dockingManager = workspaceContent?.GetDockingManager();
            if (dockingManager != null)
            {
                concreteLayoutManager.Initialize(dockingManager);
                System.Diagnostics.Debug.WriteLine("Layout manager initialized successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Warning: DockingManager is null, layout manager not initialized");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("Warning: Layout manager is not WorkspaceLayoutManager type");
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsWorkspaceVisible))
        {
            UpdateWorkspaceVisibility();
        }
    }

    private void UpdateWorkspaceVisibility()
    {
        if (_viewModel != null && workspaceContent?.GetWorkspaceAnchorable() != null)
        {
            workspaceContent.GetWorkspaceAnchorable()!.IsVisible = _viewModel.IsWorkspaceVisible;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }
        base.OnClosed(e);
    }
}
