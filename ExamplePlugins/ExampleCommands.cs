using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArtStudio.Core;
using ArtStudio.Core.Services;
using Microsoft.Extensions.Logging;

namespace ArtStudio.ExamplePlugins;

/// <summary>
/// Example implementation of a plugin command for creating a new document
/// </summary>
public class NewDocumentCommand : PluginCommandBase
{
    public override string CommandId => "new-document";
    public override string DisplayName => "New Document";
    public override string Description => "Create a new document";
    public override string? DetailedInstructions => "Creates a new document with the specified width, height, and background color.";
    public override string? IconResource => "Icons/NewDocument.png";
    public override string? KeyboardShortcut => "Ctrl+N";
    public override CommandCategory Category => CommandCategory.File;
    public override int Priority => 10;

    public override IReadOnlyDictionary<string, CommandParameter> Parameters { get; } =
        new Dictionary<string, CommandParameter>
        {
            ["width"] = new CommandParameter
            {
                Name = "width",
                Type = typeof(int),
                IsRequired = false,
                DefaultValue = 800,
                Description = "Document width in pixels"
            },
            ["height"] = new CommandParameter
            {
                Name = "height",
                Type = typeof(int),
                IsRequired = false,
                DefaultValue = 600,
                Description = "Document height in pixels"
            },
            ["backgroundColor"] = new CommandParameter
            {
                Name = "backgroundColor",
                Type = typeof(string),
                IsRequired = false,
                DefaultValue = "White",
                Description = "Background color (White, Black, Transparent)",
                ValidValues = new[] { "White", "Black", "Transparent" }
            }
        };

    public NewDocumentCommand(ILogger<NewDocumentCommand>? logger = null) : base(logger)
    {
    }

    protected override bool OnCanExecute(ICommandContext context, IDictionary<string, object>? parameters)
    {
        // Can always create a new document
        return IsEnabled;
    }

    protected override async Task<CommandResult> OnExecuteAsync(
        ICommandContext context,
        IDictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get parameters
            var width = GetParameter(parameters, "width", 800);
            var height = GetParameter(parameters, "height", 600);
            var backgroundColor = GetParameter(parameters, "backgroundColor", "White");

            Logger?.LogInformation("Creating new document: {Width}x{Height}, background: {BackgroundColor}",
                width, height, backgroundColor);

            // Report progress
            ReportProgress(context, 0, "Initializing new document...");

            // Simulate some work
            await Task.Delay(100, cancellationToken);
            ThrowIfCancelled(cancellationToken);

            ReportProgress(context, 50, "Setting up canvas...");

            // Here you would actually create the document using the editor service
            // For example:
            // await context.EditorService.CreateNewDocumentAsync(width, height, backgroundColor, cancellationToken);

            await Task.Delay(100, cancellationToken);
            ThrowIfCancelled(cancellationToken);

            ReportProgress(context, 100, "Document created successfully");

            return CommandResult.Success($"New document created ({width}x{height})",
                new Dictionary<string, object>
                {
                    ["width"] = width!,
                    ["height"] = height!,
                    ["backgroundColor"] = backgroundColor!
                });
        }
        catch (OperationCanceledException)
        {
            throw; // Re-throw cancellation
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to create new document");
            return CommandResult.Failure("Failed to create new document", ex);
        }
    }

    public override Task PrepareAsync(ICommandContext context, CancellationToken cancellationToken = default)
    {
        Logger?.LogDebug("Preparing new document command");
        // Could validate editor state, check available memory, etc.
        return base.PrepareAsync(context, cancellationToken);
    }

    public override Task CleanupAsync(ICommandContext context, CancellationToken cancellationToken = default)
    {
        Logger?.LogDebug("Cleaning up new document command");
        // Could cleanup temporary resources, etc.
        return base.CleanupAsync(context, cancellationToken);
    }
}

/// <summary>
/// Example implementation of a save document command
/// </summary>
public class SaveDocumentCommand : PluginCommandBase
{
    public override string CommandId => "save-document";
    public override string DisplayName => "Save Document";
    public override string Description => "Save the current document";
    public override string? IconResource => "Icons/Save.png";
    public override string? KeyboardShortcut => "Ctrl+S";
    public override CommandCategory Category => CommandCategory.File;
    public override int Priority => 20;

    public override IReadOnlyDictionary<string, CommandParameter>? Parameters { get; } =
        new Dictionary<string, CommandParameter>
        {
            ["filePath"] = new CommandParameter
            {
                Name = "filePath",
                Type = typeof(string),
                IsRequired = false,
                Description = "File path to save to (if not specified, uses current document path)"
            }
        };

    public SaveDocumentCommand(ILogger<SaveDocumentCommand>? logger = null) : base(logger)
    {
    }

    protected override bool OnCanExecute(ICommandContext context, IDictionary<string, object>? parameters)
    {
        // Can only save if there's an active document
        // This would check the editor service for an active document
        return IsEnabled; // Simplified for example
    }

    protected override async Task<CommandResult> OnExecuteAsync(
        ICommandContext context,
        IDictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            var filePath = GetParameter<string>(parameters, "filePath");

            Logger?.LogInformation("Saving document to: {FilePath}", filePath ?? "current path");

            ReportProgress(context, 0, "Preparing to save...");

            // Simulate save operation
            await Task.Delay(200, cancellationToken);
            ThrowIfCancelled(cancellationToken);

            ReportProgress(context, 50, "Writing file data...");

            // Here you would actually save using the editor service
            // await context.EditorService.SaveDocumentAsync(filePath, cancellationToken);

            await Task.Delay(200, cancellationToken);
            ThrowIfCancelled(cancellationToken);

            ReportProgress(context, 100, "Document saved successfully");

            return CommandResult.Success("Document saved successfully");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to save document");
            return CommandResult.Failure("Failed to save document", ex);
        }
    }
}

/// <summary>
/// Example implementation of a filter command
/// </summary>
public class SampleFilterCommand : PluginCommandBase
{
    public override string CommandId => "apply-blur-filter";
    public override string DisplayName => "Apply Blur Filter";
    public override string Description => "Apply a blur effect to the current layer";
    public override string? DetailedInstructions => "Applies a Gaussian blur filter to the active layer with configurable radius.";
    public override string? IconResource => "Icons/Blur.png";
    public override CommandCategory Category => CommandCategory.Filter;
    public override int Priority => 100;

    public override IReadOnlyDictionary<string, CommandParameter>? Parameters { get; } =
        new Dictionary<string, CommandParameter>
        {
            ["radius"] = new CommandParameter
            {
                Name = "radius",
                Type = typeof(double),
                IsRequired = false,
                DefaultValue = 5.0,
                Description = "Blur radius in pixels (0.1 to 100.0)"
            },
            ["previewMode"] = new CommandParameter
            {
                Name = "previewMode",
                Type = typeof(bool),
                IsRequired = false,
                DefaultValue = false,
                Description = "Whether to apply the filter in preview mode"
            }
        };

    public SampleFilterCommand(ILogger<SampleFilterCommand>? logger = null) : base(logger)
    {
    }

    protected override bool OnCanExecute(ICommandContext context, IDictionary<string, object>? parameters)
    {
        // Can only apply filter if there's an active document with a selected layer
        // This would check the editor service for an active document and layer
        return IsEnabled; // Simplified for example
    }

    protected override async Task<CommandResult> OnExecuteAsync(
        ICommandContext context,
        IDictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            var radius = GetParameter(parameters, "radius", 5.0);
            var previewMode = GetParameter(parameters, "previewMode", false);

            // Validate radius
            if (radius < 0.1 || radius > 100.0)
            {
                return CommandResult.Failure("Blur radius must be between 0.1 and 100.0");
            }

            Logger?.LogInformation("Applying blur filter with radius: {Radius}, preview: {PreviewMode}",
                radius, previewMode);

            ReportProgress(context, 0, "Preparing filter...");

            // Simulate filter preparation
            await Task.Delay(100, cancellationToken);
            ThrowIfCancelled(cancellationToken);

            ReportProgress(context, 25, "Analyzing image data...");

            // Simulate image analysis
            await Task.Delay(200, cancellationToken);
            ThrowIfCancelled(cancellationToken);

            ReportProgress(context, 50, "Applying blur effect...");

            // Here you would actually apply the blur filter
            // await context.EditorService.ApplyFilterAsync("blur", new { radius, previewMode }, cancellationToken);

            // Simulate filter processing
            await Task.Delay(500, cancellationToken);
            ThrowIfCancelled(cancellationToken);

            ReportProgress(context, 100, "Filter applied successfully");

            var resultMessage = previewMode ? "Blur filter applied in preview mode" : "Blur filter applied";
            return CommandResult.Success(resultMessage, new Dictionary<string, object>
            {
                ["radius"] = radius!,
                ["previewMode"] = previewMode!,
                ["filterType"] = "blur"
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to apply blur filter");
            return CommandResult.Failure("Failed to apply blur filter", ex);
        }
    }
}
