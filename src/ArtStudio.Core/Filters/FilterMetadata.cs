using System;
using System.Collections.Generic;

namespace ArtStudio.Core;

/// <summary>
/// Filter metadata
/// </summary>
public class FilterMetadata
{
    public TimeSpan ProcessingTime { get; set; }
    public Dictionary<string, object> Properties { get; } = new();
}
