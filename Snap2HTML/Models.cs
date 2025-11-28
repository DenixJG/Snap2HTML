namespace Snap2HTML;

public class SnapSettings
{
    public string RootFolder { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string OutputFile { get; set; } = string.Empty;
    public bool SkipHiddenItems { get; set; } = true;
    public bool SkipSystemItems { get; set; } = true;
    public bool OpenInBrowser { get; set; }
    public bool LinkFiles { get; set; }
    public string LinkRoot { get; set; } = string.Empty;
}

public class SnappedFile
{
    public SnappedFile(string name)
    {
        Name = name;
        Properties = [];
    }

    public string Name { get; set; }
    public Dictionary<string, string> Properties { get; set; }

    public string GetProp(string key) =>
        Properties.TryGetValue(key, out var value) ? value : string.Empty;
}

public class SnappedFolder
{
    public SnappedFolder(string name, string path)
    {
        Name = name;
        Path = path;
        Properties = [];
        Files = [];
    }

    public string Name { get; set; }
    public string Path { get; set; }
    public Dictionary<string, string> Properties { get; set; }
    public List<SnappedFile> Files { get; set; }

    public string GetFullPath()
    {
        var path = Path.EndsWith(@"\")
            ? Path + Name
            : Path + @"\" + Name;

        // Remove trailing backslash except for drive letters
        if (path.EndsWith(@"\") && !Utils.IsWildcardMatch(@"?:\", path, false))
        {
            path = path[..^1];
        }

        return path;
    }

    public string GetProp(string key) =>
        Properties.TryGetValue(key, out var value) ? value : string.Empty;
}
