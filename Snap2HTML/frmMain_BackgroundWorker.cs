using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace Snap2HTML;

public partial class frmMain : Form
{
    // This runs on a separate thread from the GUI
    private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
    {
        var settings = e.Argument as SnapSettings;
        if (settings == null)
        {
            backgroundWorker.ReportProgress(0, "Error: Invalid settings");
            return;
        }

        // Get files & folders
        var content = GetContent(settings, backgroundWorker);

        if (backgroundWorker.CancellationPending)
        {
            backgroundWorker.ReportProgress(0, "User cancelled");
            return;
        }

        if (content == null)
        {
            backgroundWorker.ReportProgress(0, "Error reading source");
            return;
        }

        // Calculate some stats
        var totDirs = 0;
        var totFiles = 0;
        long totSize = 0;

        foreach (var folder in content)
        {
            totDirs++;
            foreach (var file in folder.Files)
            {
                totFiles++;
                totSize += Utils.ParseLong(file.GetProp("Size"));
            }
        }

        // Let's generate the output
        backgroundWorker.ReportProgress(0, "Generating HTML file...");

        // Read template
        var sbTemplate = new StringBuilder();
        try
        {
            var templatePath = Path.Combine(
                Path.GetDirectoryName(Application.ExecutablePath) ?? string.Empty,
                "template.html");

            using var reader = new StreamReader(templatePath, Encoding.UTF8);
            sbTemplate.Append(reader.ReadToEnd());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open 'Template.html' for reading:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            backgroundWorker.ReportProgress(0, "An error occurred...");
            return;
        }

        // Build HTML
        sbTemplate.Replace("[TITLE]", settings.Title);
        sbTemplate.Replace("[APP LINK]", "http://www.rlvision.com");
        sbTemplate.Replace("[APP NAME]", Application.ProductName);

        var versionParts = Application.ProductVersion.Split('.');
        sbTemplate.Replace("[APP VER]", $"{versionParts[0]}.{versionParts[1]}");
        sbTemplate.Replace("[GEN TIME]", DateTime.Now.ToString("t"));
        sbTemplate.Replace("[GEN DATE]", DateTime.Now.ToString("d"));
        sbTemplate.Replace("[NUM FILES]", totFiles.ToString());
        sbTemplate.Replace("[NUM DIRS]", totDirs.ToString());
        sbTemplate.Replace("[TOT SIZE]", totSize.ToString());

        if (settings.LinkFiles)
        {
            sbTemplate.Replace("[LINK FILES]", "true");
            sbTemplate.Replace("[LINK ROOT]", settings.LinkRoot.Replace(@"\", "/"));
            sbTemplate.Replace("[SOURCE ROOT]", settings.RootFolder.Replace(@"\", "/"));

            var linkRoot = settings.LinkRoot.Replace(@"\", "/");

            // "file://" is needed in the browser if path begins with drive letter, else it should not be used
            if (Utils.IsWildcardMatch(@"?:/*", linkRoot, false))
            {
                sbTemplate.Replace("[LINK PROTOCOL]", @"file://");
            }
            else if (linkRoot.StartsWith("//")) // For UNC paths e.g. \\server\path
            {
                sbTemplate.Replace("[LINK PROTOCOL]", @"file://///");
            }
            else
            {
                sbTemplate.Replace("[LINK PROTOCOL]", "");
            }
        }
        else
        {
            sbTemplate.Replace("[LINK FILES]", "false");
            sbTemplate.Replace("[LINK PROTOCOL]", "");
            sbTemplate.Replace("[LINK ROOT]", "");
            sbTemplate.Replace("[SOURCE ROOT]", settings.RootFolder.Replace(@"\", "/"));
        }

        // Write output file
        try
        {
            using var writer = new StreamWriter(settings.OutputFile, false, Encoding.UTF8) { AutoFlush = true };

            var template = sbTemplate.ToString();
            var startOfData = template.IndexOf("[DIR DATA]");

            writer.Write(template[..startOfData]);

            BuildJavascriptContentArray(content, 0, writer, backgroundWorker);

            if (backgroundWorker.CancellationPending)
            {
                backgroundWorker.ReportProgress(0, "User cancelled");
                return;
            }

            writer.Write(template[(startOfData + 10)..]);

            if (settings.OpenInBrowser)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = settings.OutputFile,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open file for writing:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            backgroundWorker.ReportProgress(0, "An error occurred...");
            return;
        }

        // Ready!
        Cursor.Current = Cursors.Default;
        backgroundWorker.ReportProgress(100, "Ready!");
    }

    // --- Helper functions (must be static to avoid thread problems) ---

    private static List<SnappedFolder>? GetContent(SnapSettings settings, BackgroundWorker bgWorker)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var result = new List<SnappedFolder>();

        // Get all folders
        var dirs = new List<string> { settings.RootFolder };
        DirSearch(settings.RootFolder, dirs, settings.SkipHiddenItems, settings.SkipSystemItems, stopwatch, bgWorker);
        dirs = Utils.SortDirList(dirs);

        if (bgWorker.CancellationPending)
        {
            return null;
        }

        var totFiles = 0;
        stopwatch.Restart();

        try
        {
            // Parse each folder
            for (var d = 0; d < dirs.Count; d++)
            {
                // Get folder properties
                var dirName = dirs[d];
                var currentDir = dirName == Path.GetPathRoot(dirName)
                    ? new SnappedFolder("", dirName)
                    : new SnappedFolder(Path.GetFileName(dirName), Path.GetDirectoryName(dirName) ?? string.Empty);

                var modifiedDate = "";
                var createdDate = "";

                try
                {
                    modifiedDate = Utils.ToUnixTimestamp(Directory.GetLastWriteTime(dirName).ToLocalTime()).ToString();
                    createdDate = Utils.ToUnixTimestamp(Directory.GetCreationTime(dirName).ToLocalTime()).ToString();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex} Exception caught.");
                }

                currentDir.Properties.Add("Modified", modifiedDate);
                currentDir.Properties.Add("Created", createdDate);

                // Get files in folder
                List<string> files;
                try
                {
                    files = [.. Directory.GetFiles(dirName, "*.*", SearchOption.TopDirectoryOnly)];
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex} Exception caught.");
                    result.Add(currentDir);
                    continue;
                }

                files.Sort();

                // Get file properties
                foreach (var sFile in files)
                {
                    totFiles++;

                    if (stopwatch.ElapsedMilliseconds >= 50)
                    {
                        bgWorker.ReportProgress(0, $"Reading files... {totFiles} ({sFile})");
                        stopwatch.Restart();
                    }

                    if (bgWorker.CancellationPending)
                    {
                        return null;
                    }

                    var currentFile = new SnappedFile(Path.GetFileName(sFile));

                    try
                    {
                        var fi = new FileInfo(sFile);
                        var isHidden = (fi.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                        var isSystem = (fi.Attributes & FileAttributes.System) == FileAttributes.System;

                        if ((isHidden && settings.SkipHiddenItems) || (isSystem && settings.SkipSystemItems))
                        {
                            continue;
                        }

                        currentFile.Properties.Add("Size", fi.Length.ToString());

                        modifiedDate = "-";
                        createdDate = "-";

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

                    currentDir.Files.Add(currentFile);
                }

                result.Add(currentDir);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex} exception caught: {ex.Message}");
        }

        return result;
    }

    // Recursive function to get all folders and subfolders of given path
    private static void DirSearch(string sDir, List<string> lstDirs, bool skipHidden, bool skipSystem, Stopwatch stopwatch, BackgroundWorker backgroundWorker)
    {
        if (backgroundWorker.CancellationPending) return;

        try
        {
            foreach (var d in Directory.GetDirectories(sDir))
            {
                var includeThisFolder = true;

                // Exclude folders that have the system or hidden attr set (if required)
                if (skipHidden || skipSystem)
                {
                    var attr = new DirectoryInfo(d).Attributes;

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
                        backgroundWorker.ReportProgress(0, $"Getting folders... {lstDirs.Count} ({d})");
                        stopwatch.Restart();
                    }

                    DirSearch(d, lstDirs, skipHidden, skipSystem, stopwatch, backgroundWorker);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in DirSearch(): {ex.Message}");
        }
    }

    private static void BuildJavascriptContentArray(List<SnappedFolder> content, int startIndex, StreamWriter writer, BackgroundWorker bgWorker)
    {
        //  Data format:
        //    Each index in "dirs" array is an array representing a directory:
        //      First item in array: "directory path*always 0*directory modified date"
        //        Note that forward slashes are used instead of (Windows style) backslashes
        //      Then, for each each file in the directory: "filename*size of file*file modified date"
        //      Second to last item in array tells the total size of directory content
        //      Last item in array references IDs to all subdirectories of this dir (if any).
        //        ID is the item index in dirs array.
        //    Note: Modified date is in UNIX format

        var lineBreakSymbol = ""; // Could be set to \n to make the html output more readable, at the expense of increased size

        // Assign an ID to each folder. This is equal to the index in the JS data array
        var dirIndexes = new Dictionary<string, string>();
        for (var i = 0; i < content.Count; i++)
        {
            dirIndexes.Add(content[i].GetFullPath(), (i + startIndex).ToString());
        }

        // Build a lookup table with subfolder IDs for each folder
        var subdirs = new Dictionary<string, List<string>>();

        foreach (var dir in content)
        {
            // Add all folders as keys
            subdirs.Add(dir.GetFullPath(), []);
        }

        if (!subdirs.ContainsKey(content[0].Path) && content[0].Name != "")
        {
            // Ensure that root folder is not missed
            subdirs.Add(content[0].Path, []);
        }

        foreach (var dir in content)
        {
            if (dir.Name != "")
            {
                try
                {
                    // For each folder, add its index to its parent folder list of subdirs
                    subdirs[dir.Path].Add(dirIndexes[dir.GetFullPath()]);
                }
                catch
                {
                    // Orphan file or folder?
                }
            }
        }

        // Generate the data array
        var result = new StringBuilder();

        foreach (var currentDir in content)
        {
            result.Append($"D.p([{lineBreakSymbol}");

            var sDirWithForwardSlash = currentDir.GetFullPath().Replace(@"\", "/");
            result.Append($"\"{Utils.MakeCleanJsString(sDirWithForwardSlash)}*0*{currentDir.GetProp("Modified")}\",{lineBreakSymbol}");

            long dirSize = 0;

            foreach (var currentFile in currentDir.Files)
            {
                result.Append($"\"{Utils.MakeCleanJsString(currentFile.Name)}*{currentFile.GetProp("Size")}*{currentFile.GetProp("Modified")}\",{lineBreakSymbol}");
                dirSize += Utils.ParseLong(currentFile.GetProp("Size"));
            }

            // Add total dir size
            result.Append($"{dirSize},{lineBreakSymbol}");

            // Add reference to subdirs
            result.Append($"\"{string.Join("*", subdirs[currentDir.GetFullPath()])}\"{lineBreakSymbol}");

            // Finalize
            result.Append("])");
            result.Append('\n');

            // Write result in chunks to limit memory consumption
            if (result.Length > 10240)
            {
                writer.Write(result.ToString());
                result.Clear();
            }

            if (bgWorker.CancellationPending)
            {
                return;
            }
        }

        writer.Write(result.ToString());
    }
}
