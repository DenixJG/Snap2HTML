namespace Snap2HTML.Services;

/// <summary>
/// Interface for providing HTML templates.
/// </summary>
public interface ITemplateProvider
{
    /// <summary>
    /// Gets the default template path.
    /// </summary>
    string DefaultTemplatePath { get; }

    /// <summary>
    /// Loads the template content asynchronously.
    /// </summary>
    /// <param name="templatePath">Optional custom template path. If null, uses the default template.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The template content as a string.</returns>
    Task<string> LoadTemplateAsync(
        string? templatePath = null,
        CancellationToken cancellationToken = default);
}
