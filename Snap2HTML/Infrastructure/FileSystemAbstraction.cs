using System.Text;

namespace Snap2HTML.Infrastructure;

/// <summary>
/// Default implementation of IFileSystemAbstraction that wraps the actual file system.
/// </summary>
public class FileSystemAbstraction : IFileSystemAbstraction
{
    public IEnumerable<string> GetDirectories(string path) =>
        Directory.GetDirectories(path);

    public IEnumerable<string> GetFiles(string path) =>
        Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);

    public FileAttributes GetAttributes(string path) =>
        File.GetAttributes(path);

    public DateTime GetLastWriteTime(string path) =>
        Directory.GetLastWriteTime(path);

    public DateTime GetCreationTime(string path) =>
        Directory.GetCreationTime(path);

    public FileInfo GetFileInfo(string path) =>
        new FileInfo(path);

    public DirectoryInfo GetDirectoryInfo(string path) =>
        new DirectoryInfo(path);

    public bool DirectoryExists(string path) =>
        Directory.Exists(path);

    public bool FileExists(string path) =>
        File.Exists(path);

    public async Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default) =>
        await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);

    public StreamWriter CreateStreamWriter(string path) =>
        new StreamWriter(path, false, Encoding.UTF8) { AutoFlush = true };
}
