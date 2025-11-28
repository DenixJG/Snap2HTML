using System.Diagnostics;
using Snap2HTML.Infrastructure;

namespace Snap2HTML.Services;

/// <summary>
/// Implementation of IFolderScanner that scans folders for files and metadata.
/// </summary>
public class FolderScanner : IFolderScanner
{
    private readonly IFileSystemAbstraction _fileSystem;

    public FolderScanner(IFileSystemAbstraction fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<ScanResult> ScanAsync(
        ScanOptions options,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = new ScanResult();

        try
        {
            var stopwatch = Stopwatch.StartNew();

            // Get all folders
            var dirs = new List<string> { options.RootFolder };
            await Task.Run(() => DirSearch(
                options.RootFolder,
                dirs,
                options.SkipHiddenItems,
                options.SkipSystemItems,
                stopwatch,
                progress,
                cancellationToken), cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                result.WasCancelled = true;
                return result;
            }

            dirs = Utils.SortDirList(dirs);

            // Parse each folder
            var totFiles = 0;
            stopwatch.Restart();

            for (var d = 0; d < dirs.Count; d++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    result.WasCancelled = true;
                    return result;
                }

                var dirName = dirs[d];
                var currentDir = CreateSnappedFolder(dirName);

                // Get folder metadata
                SetFolderMetadata(currentDir, dirName);

                // Get files in folder
                var files = GetFilesInFolder(dirName, options, stopwatch, progress, ref totFiles, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    result.WasCancelled = true;
                    return result;
                }

                currentDir.Files.AddRange(files);
                result.Folders.Add(currentDir);
            }

            // Calculate stats
            CalculateStats(result);
        }
        catch (OperationCanceledException)
        {
            result.WasCancelled = true;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private SnappedFolder CreateSnappedFolder(string dirName)
    {
        if (dirName == Path.GetPathRoot(dirName))
        {
            return new SnappedFolder("", dirName);
        }

        return new SnappedFolder(
            Path.GetFileName(dirName),
            Path.GetDirectoryName(dirName) ?? string.Empty);
    }

    private void SetFolderMetadata(SnappedFolder folder, string dirName)
    {
        var modifiedDate = "";
        var createdDate = "";

        try
        {
            modifiedDate = Utils.ToUnixTimestamp(_fileSystem.GetLastWriteTime(dirName).ToLocalTime()).ToString();
            createdDate = Utils.ToUnixTimestamp(_fileSystem.GetCreationTime(dirName).ToLocalTime()).ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex} Exception caught.");
        }

        folder.Properties.Add("Modified", modifiedDate);
        folder.Properties.Add("Created", createdDate);
    }

    private List<SnappedFile> GetFilesInFolder(
        string dirName,
        ScanOptions options,
        Stopwatch stopwatch,
        IProgress<ScanProgress>? progress,
        ref int totFiles,
        CancellationToken cancellationToken)
    {
        var result = new List<SnappedFile>();

        List<string> files;
        try
        {
            files = _fileSystem.GetFiles(dirName).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex} Exception caught.");
            return result;
        }

        files.Sort();

        foreach (var sFile in files)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return result;
            }

            totFiles++;

            if (stopwatch.ElapsedMilliseconds >= 50)
            {
                progress?.Report(new ScanProgress
                {
                    StatusMessage = $"Reading files... {totFiles}",
                    FilesProcessed = totFiles,
                    CurrentItem = sFile
                });
                stopwatch.Restart();
            }

            var snappedFile = CreateSnappedFile(sFile, options);
            if (snappedFile != null)
            {
                result.Add(snappedFile);
            }
        }

        return result;
    }

    private SnappedFile? CreateSnappedFile(string filePath, ScanOptions options)
    {
        var currentFile = new SnappedFile(Path.GetFileName(filePath));

        try
        {
            var fi = _fileSystem.GetFileInfo(filePath);
            var isHidden = (fi.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
            var isSystem = (fi.Attributes & FileAttributes.System) == FileAttributes.System;

            if ((isHidden && options.SkipHiddenItems) || (isSystem && options.SkipSystemItems))
            {
                return null;
            }

            currentFile.Properties.Add("Size", fi.Length.ToString());

            var modifiedDate = "-";
            var createdDate = "-";

            try
            {
                modifiedDate = Utils.ToUnixTimestamp(fi.LastWriteTime.ToLocalTime()).ToString();
                createdDate = Utils.ToUnixTimestamp(fi.CreationTime.ToLocalTime()).ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex} Exception caught.");
            }

            currentFile.Properties.Add("Modified", modifiedDate);
            currentFile.Properties.Add("Created", createdDate);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex} Exception caught.");
        }

        return currentFile;
    }

    private void DirSearch(
        string sDir,
        List<string> lstDirs,
        bool skipHidden,
        bool skipSystem,
        Stopwatch stopwatch,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;

        try
        {
            foreach (var d in _fileSystem.GetDirectories(sDir))
            {
                if (cancellationToken.IsCancellationRequested) return;

                var includeThisFolder = true;

                if (skipHidden || skipSystem)
                {
                    var di = _fileSystem.GetDirectoryInfo(d);
                    var attr = di.Attributes;

                    if (skipHidden && (attr & FileAttributes.Hidden) == FileAttributes.Hidden)
                    {
                        includeThisFolder = false;
                    }

                    if (skipSystem && (attr & FileAttributes.System) == FileAttributes.System)
                    {
                        includeThisFolder = false;
                    }
                }

                if (includeThisFolder)
                {
                    lstDirs.Add(d);

                    if (stopwatch.ElapsedMilliseconds >= 50)
                    {
                        progress?.Report(new ScanProgress
                        {
                            StatusMessage = $"Getting folders... {lstDirs.Count}",
                            FoldersProcessed = lstDirs.Count,
                            CurrentItem = d
                        });
                        stopwatch.Restart();
                    }

                    DirSearch(d, lstDirs, skipHidden, skipSystem, stopwatch, progress, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in DirSearch(): {ex.Message}");
        }
    }

    private static void CalculateStats(ScanResult result)
    {
        result.TotalDirectories = result.Folders.Count;
        result.TotalFiles = 0;
        result.TotalSize = 0;

        foreach (var folder in result.Folders)
        {
            foreach (var file in folder.Files)
            {
                result.TotalFiles++;
                result.TotalSize += Utils.ParseLong(file.GetProp("Size"));
            }
        }
    }
}
