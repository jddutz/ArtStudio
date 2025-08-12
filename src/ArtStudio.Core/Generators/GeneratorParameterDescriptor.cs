using System.Collections.Generic;

namespace ArtStudio.Core;

/// <summary>
/// Generator parameter descriptor for UI generation
/// </summary>
public class GeneratorParameterDescriptor
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ParameterType Type { get; set; }
    public object? DefaultValue { get; set; }
    public object? MinValue { get; set; }
    public object? MaxValue { get; set; }
    public IReadOnlyList<object>? AllowedValues { get; set; }
    public string? Unit { get; set; }
    public int DecimalPlaces { get; set; }
    public bool IsRequired { get; set; }
    public string? Group { get; set; }
}
