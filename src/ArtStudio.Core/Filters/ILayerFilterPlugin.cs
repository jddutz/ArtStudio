using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArtStudio.Core;

/// <summary>
/// Interface for layer filter/effect plugins
/// </summary>
public interface ILayerFilterPlugin : IPlugin
{
    /// <summary>
    /// Filter category for organization
    /// </summary>
    FilterCategory Category { get; }

    /// <summary>
    /// Icon resource path or identifier
    /// </summary>
    string? IconResource { get; }

    /// <summary>
    /// Whether this filter supports real-time preview
    /// </summary>
    bool SupportsPreview { get; }

    /// <summary>
    /// Whether this filter is destructive (modifies original data)
    /// </summary>
    bool IsDestructive { get; }

    /// <summary>
    /// Filter settings/parameters
    /// </summary>
    IFilterSettings Settings { get; }

    /// <summary>
    /// Apply the filter to layer data
    /// </summary>
    Task<FilterResult> ApplyAsync(LayerData inputData, IFilterSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a preview of the filter effect
    /// </summary>
    Task<FilterResult> PreviewAsync(LayerData inputData, IFilterSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the filter's parameter UI descriptor
    /// </summary>
    FilterParameterDescriptor[] GetParameterDescriptors();
}
