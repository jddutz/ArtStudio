using System;
using System.Collections.Generic;

namespace ArtStudio.Core;

/// <summary>
/// Import metadata
/// </summary>
public class ImportMetadata
{
    public Dictionary<string, object> Properties { get; set; } = new();
    public string? ColorProfile { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
