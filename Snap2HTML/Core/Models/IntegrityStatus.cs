namespace Snap2HTML.Core.Models;

/// <summary>
/// Represents the integrity status of a file after validation.
/// </summary>
public enum IntegrityStatus
{
    /// <summary>
    /// File has not been validated.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// File passed integrity validation.
    /// </summary>
    Valid = 1,

    /// <summary>
    /// File has invalid magic bytes (file signature doesn't match expected image format).
    /// </summary>
    InvalidMagicBytes = 2,

    /// <summary>
    /// File failed decoding (corrupt or invalid image data).
    /// </summary>
    DecodingFailed = 3,

    /// <summary>
    /// File is not an image type (not validated).
    /// </summary>
    NotAnImage = 4
}
