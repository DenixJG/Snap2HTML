using Snap2HTML.Core.Utilities;

namespace Snap2HTML.Core.Models;

/// <summary>
/// Represents a folder captured during directory scanning.
/// </summary>
public class SnappedFolder
{
    /// <summary>
    /// Creates a new instance of SnappedFolder.
    /// </summary>
    /// <param name="name">The folder name.</param>
    /// <param name="path">The parent path of this folder.</param>
    public SnappedFolder(string name, string path)
    {
        Name = name;
        Path = path;
        Properties = [];
        Files = [];
    }

    /// <summary>
    /// The folder name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The parent path of this folder.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Additional properties for the folder (Modified, Created, etc.).
    /// </summary>
    public Dictionary<string, string> Properties { get; set; }

    /// <summary>
    /// The list of files in this folder.
    /// </summary>
    public List<SnappedFile> Files { get; set; }

    /// <summary>
    /// Gets the full path of this folder.
    /// </summary>
    public string GetFullPath()
    {
        var path = Path.EndsWith(@"\")
            ? Path + Name
            : Path + @"\" + Name;

        // Remove trailing backslash except for drive letters
        if (path.EndsWith(@"\") && !StringUtils.IsWildcardMatch(@"?:\", path, false))
        {
            path = path[..^1];
        }

        return path;
    }

    /// <summary>
    /// Gets a property value by key, or empty string if not found.
    /// </summary>
    public string GetProp(string key) =>
        Properties.TryGetValue(key, out var value) ? value : string.Empty;
}
