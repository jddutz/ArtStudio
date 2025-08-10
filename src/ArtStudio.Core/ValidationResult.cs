using System.Collections.Generic;

namespace ArtStudio.Core;

/// <summary>
/// Validation result for generation parameters
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
    public List<ValidationWarning> Warnings { get; set; } = new();
}
