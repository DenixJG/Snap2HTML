using Snap2HTML.Core.Models;

namespace Snap2HTML.Services.Validation;

/// <summary>
/// Interface for image integrity validation.
/// </summary>
public interface IImageIntegrityValidator
{
    /// <summary>
    /// Validates the integrity of a single image file.
    /// </summary>
    /// <param name="filePath">The path to the file to validate.</param>
    /// <param name="level">The validation level to use.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The integrity status of the file.</returns>
    ValueTask<IntegrityStatus> ValidateAsync(
        string filePath,
        IntegrityValidationLevel level,
        CancellationToken ct);

    /// <summary>
    /// Validates the integrity of multiple image files in batch.
    /// </summary>
    /// <param name="files">The file paths to validate.</param>
    /// <param name="level">The validation level to use.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async enumerable of path and status tuples.</returns>
    IAsyncEnumerable<(string Path, IntegrityStatus Status)> ValidateBatchAsync(
        IEnumerable<string> files,
        IntegrityValidationLevel level,
        CancellationToken ct);
}
