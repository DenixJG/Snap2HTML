namespace Snap2HTML.Core.Models;

/// <summary>
/// Represents a file captured during directory scanning.
/// Uses typed properties instead of Dictionary for better performance and memory efficiency.
/// </summary>
public readonly struct SnappedFile
{
    /// <summary>
    /// Creates a new instance of SnappedFile.
    /// </summary>
    /// <param name="name">The file name.</param>
    /// <param name="size">The file size in bytes.</param>
    /// <param name="modifiedTimestamp">The modified date as Unix timestamp.</param>
    /// <param name="createdTimestamp">The created date as Unix timestamp.</param>
    /// <param name="hash">The file hash (SHA256), or empty string if not computed.</param>
    public SnappedFile(string name, long size, long modifiedTimestamp, long createdTimestamp, string hash = "")
    {
        Name = name;
        Size = size;
        ModifiedTimestamp = modifiedTimestamp;
        CreatedTimestamp = createdTimestamp;
        Hash = hash;
    }

    /// <summary>
    /// The file name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The file size in bytes.
    /// </summary>
    public long Size { get; }

    /// <summary>
    /// The modified date as Unix timestamp.
    /// </summary>
    public long ModifiedTimestamp { get; }

    /// <summary>
    /// The created date as Unix timestamp.
    /// </summary>
    public long CreatedTimestamp { get; }

    /// <summary>
    /// The file hash (SHA256), or empty string if not computed.
    /// </summary>
    public string Hash { get; }

    /// <summary>
    /// Gets a property value by key for backward compatibility.
    /// </summary>
    public string GetProp(string key) => key switch
    {
        "Size" => Size.ToString(),
        "Modified" => ModifiedTimestamp.ToString(),
        "Created" => CreatedTimestamp.ToString(),
        "Hash" => Hash,
        _ => string.Empty
    };
}
