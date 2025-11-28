using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Snap2HTML.Infrastructure;

/// <summary>
/// Implementation of IFileHasher using SHA256 with parallel processing support.
/// </summary>
public class FileHasher : IFileHasher
{
    private readonly IFileSystemAbstraction _fileSystem;

    public FileHasher(IFileSystemAbstraction fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<FileHashResult> ComputeHashAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var fileInfo = _fileSystem.GetFileInfo(filePath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        using var stream = fileInfo.OpenRead();
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

        return new FileHashResult(filePath, hash, "SHA256");
    }

    public async IAsyncEnumerable<FileHashResult> ComputeHashesAsync(
        IEnumerable<string> filePaths,
        HashingOptions options,
        IProgress<int>? progress = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var files = filePaths.ToList();
        var processedCount = 0;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        var results = new System.Collections.Concurrent.ConcurrentQueue<FileHashResult>();

        await Parallel.ForEachAsync(files, parallelOptions, async (filePath, ct) =>
        {
            try
            {
                var result = await ComputeHashAsync(filePath, ct);
                results.Enqueue(result);

                var count = Interlocked.Increment(ref processedCount);
                progress?.Report(count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Log or handle individual file errors without stopping the entire operation
                Console.WriteLine($"Error hashing file {filePath}: {ex.Message}");
            }
        });

        while (results.TryDequeue(out var result))
        {
            yield return result;
        }
    }
}
