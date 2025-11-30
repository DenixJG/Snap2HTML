using Snap2HTML.Services.Scanning;

namespace Snap2HTML.Services.Generation;

/// <summary>
/// Options for HTML generation.
/// </summary>
public class HtmlGenerationOptions
{
    /// <summary>
    /// The title for the HTML output.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The output file path.
    /// </summary>
    public string OutputFile { get; set; } = string.Empty;

    /// <summary>
    /// The root folder that was scanned.
    /// </summary>
    public string RootFolder { get; set; } = string.Empty;

    /// <summary>
    /// Whether to link files in the output.
    /// </summary>
    public bool LinkFiles { get; set; }

    /// <summary>
    /// The root path for file links.
    /// </summary>
    public string LinkRoot { get; set; } = string.Empty;

    /// <summary>
    /// Whether to open the output in the browser after generation.
    /// </summary>
    public bool OpenInBrowser { get; set; }

    /// <summary>
    /// The application name for display in the output.
    /// </summary>
    public string AppName { get; set; } = string.Empty;

    /// <summary>
    /// The application version for display in the output.
    /// </summary>
    public string AppVersion { get; set; } = string.Empty;
}

/// <summary>
/// Progress information for HTML generation.
/// </summary>
public class HtmlGenerationProgress
{
    /// <summary>
    /// The current status message.
    /// </summary>
    public string StatusMessage { get; set; } = string.Empty;

    /// <summary>
    /// The percentage complete (0-100).
    /// </summary>
    public int PercentComplete { get; set; }
}

/// <summary>
/// Result of HTML generation.
/// </summary>
public class HtmlGenerationResult
{
    /// <summary>
    /// Whether the generation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Whether the generation was cancelled.
    /// </summary>
    public bool WasCancelled { get; set; }

    /// <summary>
    /// Any error message if generation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The path to the generated file.
    /// </summary>
    public string? OutputPath { get; set; }
}

/// <summary>
/// Interface for generating HTML output from scanned content.
/// </summary>
public interface IHtmlGenerator
{
    /// <summary>
    /// Generates HTML output from the scan result.
    /// </summary>
    /// <param name="scanResult">The result from folder scanning.</param>
    /// <param name="options">Options for HTML generation.</param>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the HTML generation.</returns>
    Task<HtmlGenerationResult> GenerateAsync(
        ScanResult scanResult,
        HtmlGenerationOptions options,
        IProgress<HtmlGenerationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
