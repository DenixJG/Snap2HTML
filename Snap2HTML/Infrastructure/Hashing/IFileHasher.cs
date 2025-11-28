namespace Snap2HTML.Infrastructure.Hashing;

/// <summary>
/// Options for file hashing operations.
/// </summary>
public class HashingOptions
{
    /// <summary>
    /// Whether to compute file hashes during scanning.
    /// </summary>
    public bool EnableHashing { get; set; }

    /// <summary>
    /// The hash algorithm to use (default: SHA256).
    /// </summary>
    public string Algorithm { get; set; } = "SHA256";

    /// <summary>
    /// Maximum degree of parallelism for hashing operations.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
}

/// <summary>
/// Result of a file hash operation.
/// </summary>
public record FileHashResult(string FilePath, string Hash, string Algorithm);

/// <summary>
/// Interface for computing file hashes with parallel processing support.
/// </summary>
public interface IFileHasher
{
    /// <summary>
    /// Computes the hash of a single file asynchronously.
    /// </summary>
    Task<FileHashResult> ComputeHashAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes hashes for multiple files in parallel.
    /// </summary>
    IAsyncEnumerable<FileHashResult> ComputeHashesAsync(
        IEnumerable<string> filePaths,
        HashingOptions options,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
}
