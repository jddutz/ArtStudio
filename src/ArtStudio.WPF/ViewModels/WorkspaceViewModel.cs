using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ArtStudio.Core;
using ArtStudio.WPF.Services;
using Microsoft.Extensions.Logging;

namespace ArtStudio.WPF.ViewModels;

/// <summary>
/// View model for workspace management
/// </summary>
public class WorkspaceViewModel : INotifyPropertyChanged
{
    private readonly IWorkspaceManager _workspaceManager;
    private readonly ILogger<WorkspaceViewModel>? _logger;
    private WorkspaceConfiguration? _activeWorkspace;
    private bool _isLoading;

    public WorkspaceViewModel(
        IWorkspaceManager workspaceManager,
        ILogger<WorkspaceViewModel>? logger = null)
    {
        _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
        _logger = logger;

        // Subscribe to workspace events
        _workspaceManager.WorkspaceChanged += OnWorkspaceChanged;
        _workspaceManager.WorkspaceCreated += OnWorkspaceCreated;
        _workspaceManager.WorkspaceDeleted += OnWorkspaceDeleted;

        // Initialize commands
        InitializeCommands();

        // Load workspaces
        _ = LoadWorkspacesAsync();
    }

    #region Properties

    /// <summary>
    /// All available workspaces
    /// </summary>
    public ObservableCollection<WorkspaceConfiguration> Workspaces { get; } = new();

    /// <summary>
    /// Built-in workspaces
    /// </summary>
    public ObservableCollection<WorkspaceConfiguration> BuiltInWorkspaces { get; } = new();

    /// <summary>
    /// Custom workspaces
    /// </summary>
    public ObservableCollection<WorkspaceConfiguration> CustomWorkspaces { get; } = new();

    /// <summary>
    /// Currently active workspace
    /// </summary>
    public WorkspaceConfiguration? ActiveWorkspace
    {
        get => _activeWorkspace;
        private set => SetProperty(ref _activeWorkspace, value);
    }

    /// <summary>
    /// Whether the view model is loading
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Command to switch to a workspace
    /// </summary>
    public ICommand SwitchWorkspaceCommand { get; private set; } = null!;

    /// <summary>
    /// Command to create a new workspace
    /// </summary>
    public ICommand CreateWorkspaceCommand { get; private set; } = null!;

    /// <summary>
    /// Command to create a workspace from current layout
    /// </summary>
    public ICommand CreateFromCurrentCommand { get; private set; } = null!;

    /// <summary>
    /// Command to delete a workspace
    /// </summary>
    public ICommand DeleteWorkspaceCommand { get; private set; } = null!;

    /// <summary>
    /// Command to duplicate a workspace
    /// </summary>
    public ICommand DuplicateWorkspaceCommand { get; private set; } = null!;

    /// <summary>
    /// Command to reset a workspace
    /// </summary>
    public ICommand ResetWorkspaceCommand { get; private set; } = null!;

    /// <summary>
    /// Command to update current workspace
    /// </summary>
    public ICommand UpdateCurrentWorkspaceCommand { get; private set; } = null!;

    /// <summary>
    /// Command to refresh workspaces
    /// </summary>
    public ICommand RefreshCommand { get; private set; } = null!;

    #endregion

    #region Command Implementations

    private void InitializeCommands()
    {
        SwitchWorkspaceCommand = new RelayCommand<WorkspaceConfiguration>(
            async workspace => await SwitchWorkspaceAsync(workspace),
            workspace => workspace != null && workspace != ActiveWorkspace && !IsLoading);

        CreateWorkspaceCommand = new RelayCommand<string>(
            async name => await CreateWorkspaceAsync(name),
            name => !string.IsNullOrWhiteSpace(name) && !IsLoading);

        CreateFromCurrentCommand = new RelayCommand<string>(
            async name => await CreateFromCurrentAsync(name),
            name => !string.IsNullOrWhiteSpace(name) && !IsLoading);

        DeleteWorkspaceCommand = new RelayCommand<WorkspaceConfiguration>(
            async workspace => await DeleteWorkspaceAsync(workspace),
            workspace => workspace != null && !workspace.IsBuiltIn && workspace != ActiveWorkspace && !IsLoading);

        DuplicateWorkspaceCommand = new RelayCommand<WorkspaceConfiguration>(
            async workspace => await DuplicateWorkspaceAsync(workspace),
            workspace => workspace != null && !IsLoading);

        ResetWorkspaceCommand = new RelayCommand<WorkspaceConfiguration>(
            async workspace => await ResetWorkspaceAsync(workspace),
            workspace => workspace != null && !IsLoading);

        UpdateCurrentWorkspaceCommand = new RelayCommand(
            async () => await UpdateCurrentWorkspaceAsync(),
            () => ActiveWorkspace != null && !IsLoading);

        RefreshCommand = new RelayCommand(
            async () => await LoadWorkspacesAsync(),
            () => !IsLoading);
    }

    private async Task SwitchWorkspaceAsync(WorkspaceConfiguration? workspace)
    {
        if (workspace == null || IsLoading)
            return;

        try
        {
            IsLoading = true;
            var success = await _workspaceManager.SwitchToWorkspaceAsync(workspace.Id);

            if (!success)
            {
                _logger?.LogWarning("Failed to switch to workspace: {WorkspaceName}", workspace.Name);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error switching to workspace: {WorkspaceName}", workspace.Name);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CreateWorkspaceAsync(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || IsLoading)
            return;

        try
        {
            IsLoading = true;
            var workspace = await _workspaceManager.CreateWorkspaceAsync(name);
            _logger?.LogInformation("Created new workspace: {WorkspaceName}", workspace.Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating workspace: {WorkspaceName}", name);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CreateFromCurrentAsync(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || IsLoading)
            return;

        try
        {
            IsLoading = true;
            var workspace = await _workspaceManager.CreateWorkspaceFromCurrentAsync(name);
            _logger?.LogInformation("Created workspace from current layout: {WorkspaceName}", workspace.Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating workspace from current layout: {WorkspaceName}", name);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DeleteWorkspaceAsync(WorkspaceConfiguration? workspace)
    {
        if (workspace == null || workspace.IsBuiltIn || workspace == ActiveWorkspace || IsLoading)
            return;

        try
        {
            IsLoading = true;
            var success = await _workspaceManager.DeleteWorkspaceAsync(workspace.Id);

            if (success)
            {
                _logger?.LogInformation("Deleted workspace: {WorkspaceName}", workspace.Name);
            }
            else
            {
                _logger?.LogWarning("Failed to delete workspace: {WorkspaceName}", workspace.Name);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting workspace: {WorkspaceName}", workspace.Name);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DuplicateWorkspaceAsync(WorkspaceConfiguration? workspace)
    {
        if (workspace == null || IsLoading)
            return;

        try
        {
            IsLoading = true;
            var newName = $"Copy of {workspace.Name}";
            var duplicatedWorkspace = await _workspaceManager.DuplicateWorkspaceAsync(workspace.Id, newName);
            _logger?.LogInformation("Duplicated workspace: {OriginalName} -> {NewName}", workspace.Name, duplicatedWorkspace.Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error duplicating workspace: {WorkspaceName}", workspace.Name);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ResetWorkspaceAsync(WorkspaceConfiguration? workspace)
    {
        if (workspace == null || IsLoading)
            return;

        try
        {
            IsLoading = true;
            await _workspaceManager.ResetWorkspaceAsync(workspace.Id);
            _logger?.LogInformation("Reset workspace: {WorkspaceName}", workspace.Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error resetting workspace: {WorkspaceName}", workspace.Name);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task UpdateCurrentWorkspaceAsync()
    {
        if (ActiveWorkspace == null || IsLoading)
            return;

        try
        {
            IsLoading = true;
            await _workspaceManager.UpdateCurrentWorkspaceAsync();
            _logger?.LogInformation("Updated current workspace: {WorkspaceName}", ActiveWorkspace.Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating current workspace: {WorkspaceName}", ActiveWorkspace?.Name);
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Event Handlers

    private void OnWorkspaceChanged(object? sender, WorkspaceChangedEventArgs e)
    {
        ActiveWorkspace = e.CurrentWorkspace;
        _logger?.LogDebug("Active workspace changed to: {WorkspaceName}", e.CurrentWorkspace.Name);
    }

    private void OnWorkspaceCreated(object? sender, WorkspaceCreatedEventArgs e)
    {
        AddWorkspaceToCollections(e.Workspace);
        _logger?.LogDebug("Workspace created: {WorkspaceName}", e.Workspace.Name);
    }

    private void OnWorkspaceDeleted(object? sender, WorkspaceDeletedEventArgs e)
    {
        RemoveWorkspaceFromCollections(e.WorkspaceId);
        _logger?.LogDebug("Workspace deleted: {WorkspaceName}", e.WorkspaceName);
    }

    #endregion

    #region Private Methods

    private Task LoadWorkspacesAsync()
    {
        try
        {
            IsLoading = true;

            // Clear existing collections
            Workspaces.Clear();
            BuiltInWorkspaces.Clear();
            CustomWorkspaces.Clear();

            // Load workspaces from manager
            var workspaces = _workspaceManager.GetWorkspaces().ToList();
            var builtInWorkspaces = _workspaceManager.GetBuiltInWorkspaces().ToList();
            var customWorkspaces = _workspaceManager.GetCustomWorkspaces().ToList();

            // Add to collections
            foreach (var workspace in workspaces)
                Workspaces.Add(workspace);

            foreach (var workspace in builtInWorkspaces)
                BuiltInWorkspaces.Add(workspace);

            foreach (var workspace in customWorkspaces)
                CustomWorkspaces.Add(workspace);

            // Set active workspace
            ActiveWorkspace = _workspaceManager.ActiveWorkspace;

            _logger?.LogDebug("Loaded {Count} workspaces ({BuiltIn} built-in, {Custom} custom)",
                workspaces.Count, builtInWorkspaces.Count, customWorkspaces.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading workspaces");
        }
        finally
        {
            IsLoading = false;
        }

        return Task.CompletedTask;
    }

    private void AddWorkspaceToCollections(WorkspaceConfiguration workspace)
    {
        Workspaces.Add(workspace);

        if (workspace.IsBuiltIn)
        {
            BuiltInWorkspaces.Add(workspace);
        }
        else
        {
            CustomWorkspaces.Add(workspace);
        }
    }

    private void RemoveWorkspaceFromCollections(string workspaceId)
    {
        var workspace = Workspaces.FirstOrDefault(w => w.Id == workspaceId);
        if (workspace != null)
        {
            Workspaces.Remove(workspace);

            if (workspace.IsBuiltIn)
            {
                BuiltInWorkspaces.Remove(workspace);
            }
            else
            {
                CustomWorkspaces.Remove(workspace);
            }
        }
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

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

    #endregion
}
