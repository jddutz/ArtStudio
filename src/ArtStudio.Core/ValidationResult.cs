using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ArtStudio.Core;

/// <summary>
/// Validation result for generation parameters
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public Collection<ValidationError> Errors { get; } = new();
    public Collection<ValidationWarning> Warnings { get; } = new();
}
