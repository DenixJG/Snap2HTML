namespace Snap2HTML.Infrastructure;

/// <summary>
/// Abstraction over file system operations to enable testing and future extensibility.
/// </summary>
public interface IFileSystemAbstraction
{
    /// <summary>
    /// Gets all directories in the specified path.
    /// </summary>
    IEnumerable<string> GetDirectories(string path);

    /// <summary>
    /// Gets all files in the specified directory (top level only).
    /// </summary>
    IEnumerable<string> GetFiles(string path);

    /// <summary>
    /// Gets the attributes of a file or directory.
    /// </summary>
    FileAttributes GetAttributes(string path);

    /// <summary>
    /// Gets the last write time of a file or directory.
    /// </summary>
    DateTime GetLastWriteTime(string path);

    /// <summary>
    /// Gets the creation time of a file or directory.
    /// </summary>
    DateTime GetCreationTime(string path);

    /// <summary>
    /// Gets the file info for the specified path.
    /// </summary>
    FileInfo GetFileInfo(string path);

    /// <summary>
    /// Gets the directory info for the specified path.
    /// </summary>
    DirectoryInfo GetDirectoryInfo(string path);

    /// <summary>
    /// Checks if a directory exists.
    /// </summary>
    bool DirectoryExists(string path);

    /// <summary>
    /// Checks if a file exists.
    /// </summary>
    bool FileExists(string path);

    /// <summary>
    /// Reads all text from a file.
    /// </summary>
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a StreamWriter for the specified path.
    /// </summary>
    StreamWriter CreateStreamWriter(string path);
}
