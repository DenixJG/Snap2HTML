namespace Snap2HTML.Core.Models;

/// <summary>
/// Defines the level of image integrity validation.
/// </summary>
public enum IntegrityValidationLevel
{
    /// <summary>
    /// No validation is performed.
    /// </summary>
    None = 0,

    /// <summary>
    /// Validates only the magic bytes (file signature) of images.
    /// Fast but only checks if the file header matches known image formats.
    /// </summary>
    MagicBytesOnly = 1,

    /// <summary>
    /// Performs full image decoding using ImageSharp's Image.Identify().
    /// More thorough but slower than magic bytes validation.
    /// </summary>
    FullDecode = 2
}
