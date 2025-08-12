namespace ArtStudio.Core;

/// <summary>
/// Validation warning
/// </summary>
public class ValidationWarning
{
    public string Parameter { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
