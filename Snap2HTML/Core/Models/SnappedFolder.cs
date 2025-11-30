using Snap2HTML.Core.Utilities;

namespace Snap2HTML.Core.Models;

/// <summary>
/// Represents a folder captured during directory scanning.
/// Uses typed properties instead of Dictionary for better performance and memory efficiency.
/// </summary>
public sealed class SnappedFolder
{
    /// <summary>
    /// Creates a new instance of SnappedFolder.
    /// </summary>
    /// <param name="name">The folder name.</param>
    /// <param name="path">The parent path of this folder.</param>
    /// <param name="modifiedTimestamp">The modified date as Unix timestamp.</param>
    /// <param name="createdTimestamp">The created date as Unix timestamp.</param>
    public SnappedFolder(string name, string path, long modifiedTimestamp = 0, long createdTimestamp = 0)
    {
        Name = name;
        Path = path;
        ModifiedTimestamp = modifiedTimestamp;
        CreatedTimestamp = createdTimestamp;
        Files = [];
    }

    /// <summary>
    /// The folder name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The parent path of this folder.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// The modified date as Unix timestamp.
    /// </summary>
    public long ModifiedTimestamp { get; set; }

    /// <summary>
    /// The created date as Unix timestamp.
    /// </summary>
    public long CreatedTimestamp { get; set; }

    /// <summary>
    /// The list of files in this folder.
    /// </summary>
    public List<SnappedFile> Files { get; }

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
    /// Gets a property value by key for backward compatibility.
    /// </summary>
    public string GetProp(string key) => key switch
    {
        "Modified" => ModifiedTimestamp.ToString(),
        "Created" => CreatedTimestamp.ToString(),
        _ => string.Empty
    };
}
