using Snap2HTML.Infrastructure.FileSystem;

namespace Snap2HTML.Services.Generation;

/// <summary>
/// Default implementation of ITemplateProvider that loads templates from the file system.
/// </summary>
public class TemplateProvider : ITemplateProvider
{
    private readonly IFileSystemAbstraction _fileSystem;
    private readonly string _applicationPath;

    public TemplateProvider(IFileSystemAbstraction fileSystem, string applicationPath)
    {
        _fileSystem = fileSystem;
        _applicationPath = applicationPath;
    }

    public string DefaultTemplatePath => Path.Combine(_applicationPath, "template.html");

    public async Task<string> LoadTemplateAsync(
        string? templatePath = null,
        CancellationToken cancellationToken = default)
    {
        var path = templatePath ?? DefaultTemplatePath;

        if (!_fileSystem.FileExists(path))
        {
            throw new FileNotFoundException($"Template file not found: {path}", path);
        }

        return await _fileSystem.ReadAllTextAsync(path, cancellationToken);
    }
}
