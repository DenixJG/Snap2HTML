using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using SixLabors.ImageSharp;
using Snap2HTML.Core.Models;

namespace Snap2HTML.Services.Validation;

/// <summary>
/// Implementation of image integrity validation using ImageSharp.
/// Uses a Channel-based pipeline for efficient batch processing.
/// </summary>
public class ImageIntegrityValidator : IImageIntegrityValidator
{
    /// <summary>
    /// Supported image extensions for validation.
    /// </summary>
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif"
    };

    /// <summary>
    /// Magic bytes signatures for common image formats.
    /// </summary>
    private static readonly (byte[] Signature, int Offset, string Format)[] MagicSignatures =
    [
        (new byte[] { 0xFF, 0xD8, 0xFF }, 0, "JPEG"),
        (new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, 0, "PNG"),
        (new byte[] { 0x47, 0x49, 0x46, 0x38 }, 0, "GIF"),
        (new byte[] { 0x42, 0x4D }, 0, "BMP"),
        (new byte[] { 0x52, 0x49, 0x46, 0x46 }, 0, "WebP"), // RIFF
        (new byte[] { 0x49, 0x49, 0x2A, 0x00 }, 0, "TIFF LE"),
        (new byte[] { 0x4D, 0x4D, 0x00, 0x2A }, 0, "TIFF BE")
    ];

    /// <summary>
    /// WebP signature continuation (after RIFF header).
    /// </summary>
    private static readonly byte[] WebPSignature = { 0x57, 0x45, 0x42, 0x50 };

    public ValueTask<IntegrityStatus> ValidateAsync(
        string filePath,
        IntegrityValidationLevel level,
        CancellationToken ct)
    {
        if (level == IntegrityValidationLevel.None)
        {
            return new ValueTask<IntegrityStatus>(IntegrityStatus.Unknown);
        }

        var extension = Path.GetExtension(filePath);
        if (!IsImageExtension(extension))
        {
            return new ValueTask<IntegrityStatus>(IntegrityStatus.NotAnImage);
        }

        return ValidateInternalAsync(filePath, level, ct);
    }

    private async ValueTask<IntegrityStatus> ValidateInternalAsync(
        string filePath,
        IntegrityValidationLevel level,
        CancellationToken ct)
    {
        try
        {
            // Level 1: Magic bytes validation
            var magicBytesValid = await ValidateMagicBytesAsync(filePath, ct);
            if (!magicBytesValid)
            {
                return IntegrityStatus.InvalidMagicBytes;
            }

            if (level == IntegrityValidationLevel.MagicBytesOnly)
            {
                return IntegrityStatus.Valid;
            }

            // Level 2: Full decode validation using ImageSharp
            return await ValidateFullDecodeAsync(filePath, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return IntegrityStatus.DecodingFailed;
        }
    }

    public async IAsyncEnumerable<(string Path, IntegrityStatus Status)> ValidateBatchAsync(
        IEnumerable<string> files,
        IntegrityValidationLevel level,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (level == IntegrityValidationLevel.None)
        {
            yield break;
        }

        // Create a bounded channel for the producer/consumer pattern
        var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(2000)
        {
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait
        });

        var workerCount = Environment.ProcessorCount * 2;
        var results = Channel.CreateUnbounded<(string Path, IntegrityStatus Status)>();

        // Producer: push files into channel
        var producerTask = Task.Run(async () =>
        {
            try
            {
                foreach (var file in files)
                {
                    if (ct.IsCancellationRequested) break;
                    
                    var extension = Path.GetExtension(file);
                    if (IsImageExtension(extension))
                    {
                        await channel.Writer.WriteAsync(file, ct);
                    }
                }
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, ct);

        // Consumers: process files from channel
        var consumerTasks = Enumerable.Range(0, workerCount).Select(async _ =>
        {
            await foreach (var filePath in channel.Reader.ReadAllAsync(ct))
            {
                var status = await ValidateInternalAsync(filePath, level, ct);
                await results.Writer.WriteAsync((filePath, status), ct);
            }
        }).ToArray();

        // Wait for all consumers to complete and close results channel
        _ = Task.WhenAll(consumerTasks).ContinueWith(_ =>
        {
            results.Writer.Complete();
        }, ct);

        // Yield results as they become available
        await foreach (var result in results.Reader.ReadAllAsync(ct))
        {
            yield return result;
        }

        await producerTask;
        await Task.WhenAll(consumerTasks);
    }

    /// <summary>
    /// Checks if the file extension is a supported image format.
    /// </summary>
    public static bool IsImageExtension(string extension)
    {
        return SupportedExtensions.Contains(extension);
    }

    private static async ValueTask<bool> ValidateMagicBytesAsync(string filePath, CancellationToken ct)
    {
        const int bufferSize = 16;
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

        try
        {
            await using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize,
                FileOptions.SequentialScan | FileOptions.Asynchronous);

            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, bufferSize), ct);
            if (bytesRead == 0) return false;

            // Check against known signatures
            foreach (var (signature, offset, format) in MagicSignatures)
            {
                if (bytesRead < offset + signature.Length) continue;

                var match = true;
                for (var i = 0; i < signature.Length; i++)
                {
                    if (buffer[offset + i] != signature[i])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    // Additional check for WebP (RIFF header + WEBP at offset 8)
                    if (format == "WebP")
                    {
                        if (bytesRead < 12) return false;
                        var isWebP = true;
                        for (var i = 0; i < WebPSignature.Length; i++)
                        {
                            if (buffer[8 + i] != WebPSignature[i])
                            {
                                isWebP = false;
                                break;
                            }
                        }
                        return isWebP;
                    }
                    return true;
                }
            }

            return false;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async ValueTask<IntegrityStatus> ValidateFullDecodeAsync(string filePath, CancellationToken ct)
    {
        try
        {
            // Use Image.Identify which is cheaper than Image.Load
            // It only reads metadata without fully decoding the image
            var info = await SixLabors.ImageSharp.Image.IdentifyAsync(filePath, ct);
            return info != null ? IntegrityStatus.Valid : IntegrityStatus.DecodingFailed;
        }
        catch (UnknownImageFormatException)
        {
            return IntegrityStatus.DecodingFailed;
        }
        catch (InvalidImageContentException)
        {
            return IntegrityStatus.DecodingFailed;
        }
        catch (NotSupportedException)
        {
            return IntegrityStatus.DecodingFailed;
        }
    }
}
