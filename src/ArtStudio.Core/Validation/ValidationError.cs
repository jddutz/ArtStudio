namespace ArtStudio.Core;

/// <summary>
/// Validation error
/// </summary>
public class ValidationError
{
    public string Parameter { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
