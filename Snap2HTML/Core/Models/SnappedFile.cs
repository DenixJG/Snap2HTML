namespace Snap2HTML.Core.Models;

/// <summary>
/// Represents a file captured during directory scanning.
/// </summary>
public class SnappedFile
{
    /// <summary>
    /// Creates a new instance of SnappedFile.
    /// </summary>
    /// <param name="name">The file name.</param>
    public SnappedFile(string name)
    {
        Name = name;
        Properties = [];
    }

    /// <summary>
    /// The file name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Additional properties for the file (Size, Modified, Created, etc.).
    /// </summary>
    public Dictionary<string, string> Properties { get; set; }

    /// <summary>
    /// Gets a property value by key, or empty string if not found.
    /// </summary>
    public string GetProp(string key) =>
        Properties.TryGetValue(key, out var value) ? value : string.Empty;
}
