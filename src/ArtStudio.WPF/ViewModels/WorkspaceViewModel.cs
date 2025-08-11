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

    // High-performance logging delegates
    private static readonly Action<ILogger, string, Exception?> LogSwitchWorkspaceWarning =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1, nameof(LogSwitchWorkspaceWarning)),
            "Failed to switch to workspace: {WorkspaceName}");

    private static readonly Action<ILogger, string, Exception?> LogSwitchWorkspaceError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(2, nameof(LogSwitchWorkspaceError)),
            "Error switching to workspace: {WorkspaceName}");

    private static readonly Action<ILogger, string, Exception?> LogWorkspaceCreated =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(3, nameof(LogWorkspaceCreated)),
            "Created new workspace: {WorkspaceName}");

    private static readonly Action<ILogger, string, Exception?> LogCreateWorkspaceError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(4, nameof(LogCreateWorkspaceError)),
            "Error creating workspace: {WorkspaceName}");

    private static readonly Action<ILogger, string, Exception?> LogWorkspaceFromCurrentCreated =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(5, nameof(LogWorkspaceFromCurrentCreated)),
            "Created workspace from current layout: {WorkspaceName}");

    private static readonly Action<ILogger, string, Exception?> LogCreateFromCurrentError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6, nameof(LogCreateFromCurrentError)),
            "Error creating workspace from current layout: {WorkspaceName}");

    private static readonly Action<ILogger, string, Exception?> LogWorkspaceDeleted =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(7, nameof(LogWorkspaceDeleted)),
            "Deleted workspace: {WorkspaceName}");

    private static readonly Action<ILogger, string, Exception?> LogDeleteWorkspaceWarning =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(8, nameof(LogDeleteWorkspaceWarning)),
            "Failed to delete workspace: {WorkspaceName}");

    private static readonly Action<ILogger, string, Exception?> LogDeleteWorkspaceError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(9, nameof(LogDeleteWorkspaceError)),
            "Error deleting workspace: {WorkspaceName}");

    private static readonly Action<ILogger, string, string, Exception?> LogWorkspaceDuplicated =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(10, nameof(LogWorkspaceDuplicated)),
            "Duplicated workspace: {OriginalName} -> {NewName}");

    private static readonly Action<ILogger, string, Exception?> LogDuplicateWorkspaceError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(11, nameof(LogDuplicateWorkspaceError)),
            "Error duplicating workspace: {WorkspaceName}");

    private static readonly Action<ILogger, string, Exception?> LogWorkspaceReset =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(12, nameof(LogWorkspaceReset)),
            "Reset workspace: {WorkspaceName}");

    private static readonly Action<ILogger, string, Exception?> LogResetWorkspaceError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(13, nameof(LogResetWorkspaceError)),
            "Error resetting workspace: {WorkspaceName}");

    private static readonly Action<ILogger, string, Exception?> LogCurrentWorkspaceUpdated =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(14, nameof(LogCurrentWorkspaceUpdated)),
            "Updated current workspace: {WorkspaceName}");

    private static readonly Action<ILogger, string?, Exception?> LogUpdateCurrentWorkspaceError =
        LoggerMessage.Define<string?>(LogLevel.Error, new EventId(15, nameof(LogUpdateCurrentWorkspaceError)),
            "Error updating current workspace: {WorkspaceName}");

    private static readonly Action<ILogger, string, Exception?> LogActiveWorkspaceChanged =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(16, nameof(LogActiveWorkspaceChanged)),
            "Active workspace changed to: {WorkspaceName}");

    private static readonly Action<ILogger, string, Exception?> LogWorkspaceCreatedEvent =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(17, nameof(LogWorkspaceCreatedEvent)),
            "Workspace created: {WorkspaceName}");

    private static readonly Action<ILogger, string, Exception?> LogWorkspaceDeletedEvent =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(18, nameof(LogWorkspaceDeletedEvent)),
            "Workspace deleted: {WorkspaceName}");

    private static readonly Action<ILogger, int, int, int, Exception?> LogWorkspacesLoaded =
        LoggerMessage.Define<int, int, int>(LogLevel.Debug, new EventId(19, nameof(LogWorkspacesLoaded)),
            "Loaded {Count} workspaces ({BuiltIn} built-in, {Custom} custom)");

    private static readonly Action<ILogger, Exception?> LogLoadWorkspacesError =
        LoggerMessage.Define(LogLevel.Error, new EventId(20, nameof(LogLoadWorkspacesError)),
            "Error loading workspaces");

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
            async workspace => await SwitchWorkspaceAsync(workspace).ConfigureAwait(false),
            workspace => workspace != null && workspace != ActiveWorkspace && !IsLoading);

        CreateWorkspaceCommand = new RelayCommand<string>(
            async name => await CreateWorkspaceAsync(name).ConfigureAwait(false),
            name => !string.IsNullOrWhiteSpace(name) && !IsLoading);

        CreateFromCurrentCommand = new RelayCommand<string>(
            async name => await CreateFromCurrentAsync(name).ConfigureAwait(false),
            name => !string.IsNullOrWhiteSpace(name) && !IsLoading);

        DeleteWorkspaceCommand = new RelayCommand<WorkspaceConfiguration>(
            async workspace => await DeleteWorkspaceAsync(workspace).ConfigureAwait(false),
            workspace => workspace != null && !workspace.IsBuiltIn && workspace != ActiveWorkspace && !IsLoading);

        DuplicateWorkspaceCommand = new RelayCommand<WorkspaceConfiguration>(
            async workspace => await DuplicateWorkspaceAsync(workspace).ConfigureAwait(false),
            workspace => workspace != null && !IsLoading);

        ResetWorkspaceCommand = new RelayCommand<WorkspaceConfiguration>(
            async workspace => await ResetWorkspaceAsync(workspace).ConfigureAwait(false),
            workspace => workspace != null && !IsLoading);

        UpdateCurrentWorkspaceCommand = new RelayCommand(
            async () => await UpdateCurrentWorkspaceAsync().ConfigureAwait(false),
            () => ActiveWorkspace != null && !IsLoading);

        RefreshCommand = new RelayCommand(
            async () => await LoadWorkspacesAsync().ConfigureAwait(false),
            () => !IsLoading);
    }

    private async Task SwitchWorkspaceAsync(WorkspaceConfiguration? workspace)
    {
        if (workspace == null || IsLoading)
            return;

        try
        {
            IsLoading = true;
            var success = await _workspaceManager.SwitchToWorkspaceAsync(workspace.Id).ConfigureAwait(false);

            if (!success)
            {
                if (_logger != null)
                    LogSwitchWorkspaceWarning(_logger, workspace.Name, null);
            }
        }
#pragma warning disable CA1031 // Do not catch general exception types
        // Catching all exceptions to prevent UI crashes and provide user feedback
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            if (_logger != null)
                LogSwitchWorkspaceError(_logger, workspace.Name, ex);
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
            var workspace = await _workspaceManager.CreateWorkspaceAsync(name).ConfigureAwait(false);
            if (_logger != null)
                LogWorkspaceCreated(_logger, workspace.Name, null);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        // Catching all exceptions to prevent UI crashes and provide user feedback
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            if (_logger != null)
                LogCreateWorkspaceError(_logger, name, ex);
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
            var workspace = await _workspaceManager.CreateWorkspaceFromCurrentAsync(name).ConfigureAwait(false);
            if (_logger != null)
                LogWorkspaceFromCurrentCreated(_logger, workspace.Name, null);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        // Catching all exceptions to prevent UI crashes and provide user feedback
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            if (_logger != null)
                LogCreateFromCurrentError(_logger, name, ex);
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
            var success = await _workspaceManager.DeleteWorkspaceAsync(workspace.Id).ConfigureAwait(false);

            if (success)
            {
                if (_logger != null)
                    LogWorkspaceDeleted(_logger, workspace.Name, null);
            }
            else
            {
                if (_logger != null)
                    LogDeleteWorkspaceWarning(_logger, workspace.Name, null);
            }
        }
#pragma warning disable CA1031 // Do not catch general exception types
        // Catching all exceptions to prevent UI crashes and provide user feedback
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            if (_logger != null)
                LogDeleteWorkspaceError(_logger, workspace.Name, ex);
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
            var duplicatedWorkspace = await _workspaceManager.DuplicateWorkspaceAsync(workspace.Id, newName).ConfigureAwait(false);
            if (_logger != null)
                LogWorkspaceDuplicated(_logger, workspace.Name, duplicatedWorkspace.Name, null);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        // Catching all exceptions to prevent UI crashes and provide user feedback
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            if (_logger != null)
                LogDuplicateWorkspaceError(_logger, workspace.Name, ex);
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
            await _workspaceManager.ResetWorkspaceAsync(workspace.Id).ConfigureAwait(false);
            if (_logger != null)
                LogWorkspaceReset(_logger, workspace.Name, null);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        // Catching all exceptions to prevent UI crashes and provide user feedback
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            if (_logger != null)
                LogResetWorkspaceError(_logger, workspace.Name, ex);
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
            await _workspaceManager.UpdateCurrentWorkspaceAsync().ConfigureAwait(false);
            if (_logger != null)
                LogCurrentWorkspaceUpdated(_logger, ActiveWorkspace.Name, null);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        // Catching all exceptions to prevent UI crashes and provide user feedback
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            if (_logger != null)
                LogUpdateCurrentWorkspaceError(_logger, ActiveWorkspace?.Name, ex);
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
        {
            if (_logger != null)
                LogActiveWorkspaceChanged(_logger, e.CurrentWorkspace.Name, null);
        }
    }

    private void OnWorkspaceCreated(object? sender, WorkspaceCreatedEventArgs e)
    {
        AddWorkspaceToCollections(e.Workspace);
        {
            if (_logger != null)
                LogWorkspaceCreatedEvent(_logger, e.Workspace.Name, null);
        }
    }

    private void OnWorkspaceDeleted(object? sender, WorkspaceDeletedEventArgs e)
    {
        RemoveWorkspaceFromCollections(e.WorkspaceId);
        {
            if (_logger != null)
                LogWorkspaceDeletedEvent(_logger, e.WorkspaceName, null);
        }
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

            if (_logger != null)
                LogWorkspacesLoaded(_logger, workspaces.Count, builtInWorkspaces.Count, customWorkspaces.Count, null);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        // Catching all exceptions to prevent UI crashes and provide user feedback
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            if (_logger != null)
                LogLoadWorkspacesError(_logger, ex);
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
