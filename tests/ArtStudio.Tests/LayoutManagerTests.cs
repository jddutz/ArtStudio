using Xunit;
using ArtStudio.Core.Services;
using ArtStudio.Core.Interfaces;

namespace ArtStudio.Tests;

public class LayoutManagerTests
{
    [Fact]
    public void LayoutManager_ShouldImplementInterface()
    {
        // Arrange & Act
        ILayoutManager layoutManager = new LayoutManager();

        // Assert
        Assert.NotNull(layoutManager);
    }

    [Fact]
    public void SaveLayout_ShouldNotThrow()
    {
        // Arrange
        var layoutManager = new LayoutManager();

        // Act & Assert
        var exception = Record.Exception(() => layoutManager.SaveLayout("test"));
        Assert.Null(exception);
    }

    [Fact]
    public void LoadLayout_ShouldNotThrow()
    {
        // Arrange
        var layoutManager = new LayoutManager();

        // Act & Assert
        var exception = Record.Exception(() => layoutManager.LoadLayout("test"));
        Assert.Null(exception);
    }

    [Fact]
    public void ResetToDefault_ShouldNotThrow()
    {
        // Arrange
        var layoutManager = new LayoutManager();

        // Act & Assert
        var exception = Record.Exception(() => layoutManager.ResetToDefault());
        Assert.Null(exception);
    }
}
