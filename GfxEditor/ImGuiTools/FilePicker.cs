using ImGuiNET;
using Num = System.Numerics;

namespace GfxEditor.ImGuiTools;

public enum ComDlgType
{
    FileSaveDialog,
    FileOpenDialog,
    FolderSelectDialog
}

public enum ComDlgResult
{
    None = 0,
    Ok,
    Cancel
}

public static class ImGuiComDlg
{
    private static DirectoryInfo _lastCwd = null;
    private static ComDlgResult _lastResult = ComDlgResult.None;
    private static string _lastPath = string.Empty;

    /// <summary>
    /// Shows a modal popup dialog window for accessing the file system.
    /// </summary>
    /// <param name="name">Window class name, also used as the title.</param>
    /// <param name="open">Returns true if the modal dialog was closed this frame, else false.</param>
    /// <param name="type">Type of common dialog to open.</param>
    /// <param name="filter">Optional file extension filter.</param>
    /// <param name="startPath">Optional starting path.</param>
    /// <returns>Returns true if the modal dialog was closed this frame, else false.</returns>
    public static bool ShowModalDialog(string name, ref bool open, ComDlgType type, string? filter = null,
        DirectoryInfo? startPath = null)
    {
        if (!open)
            return false;

        ImGui.OpenPopup(name);

        if (ImGui.BeginPopupModal(name, ref open, ImGuiWindowFlags.NoTitleBar))
        {
            if (startPath is null || !startPath.Exists)
                startPath = _lastCwd ?? new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

            var picker = FilePicker.GetFilePicker(name, startPath, filter, type == ComDlgType.FolderSelectDialog);
            if (picker.Draw())
            {
                _lastResult = ComDlgResult.Ok;
                _lastPath = picker.FullPath;
                FilePicker.RemoveFilePicker(name);
                ImGui.EndPopup();
                open = false;
                return true;
            }

            _lastCwd = picker.CurrentFolder;
            ImGui.EndPopup();
        }

        // cancel support
        if (!ImGui.IsPopupOpen(name))
        {
            open = false;
            _lastResult = ComDlgResult.Cancel;
            _lastPath = string.Empty;
            return false;
        }

        _lastResult = ComDlgResult.None;
        _lastPath = string.Empty;
        return false;
    }

    public static ComDlgResult GetLastResult()
    {
        return _lastResult;
    }

    public static string GetLastPath()
    {
        return _lastPath;
    }

    public static string SanitizeFileName(string fileName)
    {
        var invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
        var invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
        return System.Text.RegularExpressions.Regex.Replace(fileName, invalidRegStr, "_");
    }
}

internal class FilePicker
{
    static readonly Dictionary<string, FilePicker> _filePickers = new();

    private string RootFolder;
    public DirectoryInfo CurrentFolder { get; protected set; }
    private string SelectedFile;
    private List<string> AllowedExtensions;
    private bool OnlyAllowFolders;

    public string FullPath { get; protected set; }

    public static FilePicker GetFilePicker(string clid, DirectoryInfo startingDir, string? searchFilter = null,
        bool onlyAllowFolders = false)
    {
        var startingPath = startingDir.FullName;
        if (File.Exists(startingPath))
        {
            startingPath = new FileInfo(startingPath).DirectoryName;
        }
        else if (string.IsNullOrEmpty(startingPath) || !Directory.Exists(startingPath))
        {
            startingPath = Environment.CurrentDirectory;
            if (string.IsNullOrEmpty(startingPath))
                startingPath = AppContext.BaseDirectory;
        }

        if (!_filePickers.TryGetValue(clid, out FilePicker fp))
        {
            fp = new FilePicker();
            fp.RootFolder = startingPath;
            fp.CurrentFolder = new DirectoryInfo(startingPath);
            fp.OnlyAllowFolders = onlyAllowFolders;

            if (searchFilter != null)
            {
                if (fp.AllowedExtensions != null)
                    fp.AllowedExtensions.Clear();
                else
                    fp.AllowedExtensions = new List<string>();

                fp.AllowedExtensions.AddRange(searchFilter.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
            }

            _filePickers.Add(clid, fp);
        }

        return fp;
    }

    public static void RemoveFilePicker(string clid) => _filePickers.Remove(clid);

    public bool Draw()
    {
        ImGui.Text("Current Folder: " + CurrentFolder.FullName);
        bool result = false;

        if (ImGui.BeginChildFrame(1, new Num.Vector2(400, 400)))
        {
            var di = CurrentFolder;
            if (di.Exists)
            {
                // Table Header
                ImGui.Columns(4, "fileTable"); // 4-ways, with border
                ImGui.Separator();
                ImGui.Text("Name"); ImGui.NextColumn();
                ImGui.Text("Type"); ImGui.NextColumn();
                ImGui.Text("Date Modified"); ImGui.NextColumn();
                ImGui.Text("Size"); ImGui.NextColumn();
                ImGui.Separator();

                var parentDir = di.Parent;
                if (parentDir is not null && di.FullName != RootFolder)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, (uint)System.Drawing.Color.Yellow.ToArgb());
                    {
                        if (ImGui.Selectable("../", false, ImGuiSelectableFlags.DontClosePopups))
                            CurrentFolder = parentDir;
                    }
                    ImGui.PopStyleColor();
                    ImGui.NextColumn();
                    ImGui.Text(string.Empty); ImGui.NextColumn();
                    ImGui.Text(string.Empty); ImGui.NextColumn();
                    ImGui.Text(string.Empty); ImGui.NextColumn();
                }

                //TODO: Cache This
                var fileSystemEntries = GetFileSystemEntries(di);
                foreach (var fse in fileSystemEntries)
                {
                    if (fse.IsDirectory)
                    {
                        var name = fse.Name;

                        ImGui.PushStyleColor(ImGuiCol.Text, (uint)System.Drawing.Color.Yellow.ToArgb());
                        if (ImGui.Selectable("/" + name, false, ImGuiSelectableFlags.DontClosePopups))
                            CurrentFolder = new DirectoryInfo(Path.Combine(CurrentFolder.FullName, fse.Name));
                        ImGui.PopStyleColor();
                        ImGui.NextColumn();
                    }
                    else
                    {
                        var name = fse.Name;
                        bool isSelected = SelectedFile == fse.Name;
                        if (ImGui.Selectable(name, isSelected, ImGuiSelectableFlags.DontClosePopups))
                            SelectedFile = fse.Name;

                        if (ImGui.IsMouseDoubleClicked(0))
                        {
                            FullPath = Path.Combine(CurrentFolder.FullName, SelectedFile);
                            result = true;
                            ImGui.CloseCurrentPopup();

                        }
                        ImGui.NextColumn();
                    }

                    ImGui.Text("---"); ImGui.NextColumn();
                    ImGui.Text(fse.LastModifiedUtc.ToLocalTime().ToShortDateString()); ImGui.NextColumn();
                    ImGui.Text(fse.Length.ToString()); ImGui.NextColumn();
                }
            }
        }
        ImGui.EndChildFrame();

        if (ImGui.Button("Cancel"))
        {
            result = false;
            ImGui.CloseCurrentPopup();
            return result;
        }

        if (OnlyAllowFolders)
        {
            ImGui.SameLine();
            if (ImGui.Button("Open"))
            {
                result = true;
                FullPath = CurrentFolder.FullName;
                ImGui.CloseCurrentPopup();
            }
        }
        else if (SelectedFile != null)
        {
            ImGui.SameLine();
            if (ImGui.Button("Open"))
            {
                result = true;
                FullPath = Path.Combine(CurrentFolder.FullName, SelectedFile);
                ImGui.CloseCurrentPopup();
            }
        }

        return result;
    }

    private List<FileEntry> GetFileSystemEntries(DirectoryInfo directoryInfo)
    {
        List<FileEntry> entries = new();

        foreach (var fileSystemInfo in directoryInfo.EnumerateFileSystemInfos())
        {
            if (!OnlyAllowFolders && fileSystemInfo is FileInfo fileInfo)
            {
                if (AllowedExtensions != null)
                {
                    var ext = fileInfo.Extension.ToLowerInvariant();
                    if (AllowedExtensions.Contains(ext))
                        entries.Add(new FileEntry(false, fileInfo.Name, fileInfo.LastWriteTimeUtc, fileInfo.Length));
                }
                else
                {
                    entries.Add(new FileEntry(false, fileInfo.Name, fileInfo.LastWriteTimeUtc, fileInfo.Length));
                }
            }
            else if (fileSystemInfo is DirectoryInfo subDirectoryInfo)
            {
                entries.Add(new FileEntry(true, subDirectoryInfo.Name, subDirectoryInfo.LastWriteTimeUtc, 0));
            }
        }

        return entries;
    }

    private record struct FileEntry(bool IsDirectory, string Name, DateTime LastModifiedUtc, long Length);
}