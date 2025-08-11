using Xunit;
using ArtStudio.Core.Services;
using ArtStudio.Core;
using Moq;

namespace ArtStudio.Tests;

public class WorkspaceManagerTests
{
    [Fact]
    public void WorkspaceManager_ShouldImplementInterface()
    {
        // Arrange
        var mockLayoutManager = new Mock<IWorkspaceLayoutManager>();

        // Act
        IWorkspaceManager workspaceManager = new WorkspaceManager(mockLayoutManager.Object);

        // Assert
        Assert.NotNull(workspaceManager);
    }

    [Fact]
    public async Task CreateWorkspaceAsync_ShouldNotThrow()
    {
        // Arrange
        var mockLayoutManager = new Mock<IWorkspaceLayoutManager>();
        var workspaceManager = new WorkspaceManager(mockLayoutManager.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => workspaceManager.CreateWorkspaceAsync("test"));
        Assert.Null(exception);
    }

    [Fact]
    public async Task SwitchToWorkspaceAsync_ShouldNotThrow()
    {
        // Arrange
        var mockLayoutManager = new Mock<IWorkspaceLayoutManager>();
        var workspaceManager = new WorkspaceManager(mockLayoutManager.Object);
        var workspace = await workspaceManager.CreateWorkspaceAsync("test");

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => workspaceManager.SwitchToWorkspaceAsync(workspace.Id));
        Assert.Null(exception);
    }

    [Fact]
    public async Task ResetWorkspaceAsync_ShouldNotThrow()
    {
        // Arrange
        var mockLayoutManager = new Mock<IWorkspaceLayoutManager>();
        var workspaceManager = new WorkspaceManager(mockLayoutManager.Object);
        var workspace = await workspaceManager.CreateWorkspaceAsync("test");

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => workspaceManager.ResetWorkspaceAsync(workspace.Id));
        Assert.Null(exception);
    }
}
