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

            var picker = FilePicker.GetFilePicker(name, type, startPath, filter, type == ComDlgType.FolderSelectDialog);
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
            FilePicker.RemoveFilePicker(name);
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

    private string RootFolder = String.Empty;
    public DirectoryInfo CurrentFolder { get; protected set; }
    private string SelectedFile;
    private List<string> AllowedExtensions;
    private bool OnlyAllowFolders;
    public ComDlgType _dlgType;

    public string FullPath { get; protected set; }

    public static FilePicker GetFilePicker(string clid, ComDlgType dlgType, DirectoryInfo startingDir,
        string searchFilter = null,
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
            fp._dlgType = dlgType;
            //fp.RootFolder = startingPath; //TODO: Do rooted directory properly
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
        const int maxPath = 260;
        string str = CurrentFolder.FullName;
        ImGui.PushItemWidth(-1);
        if (ImGui.InputText("##CWD", ref str, maxPath, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            var dir = new DirectoryInfo(str);
            if (dir.Exists)
            {
                CurrentFolder = dir;
            }
        }
        ImGui.PopItemWidth();
        bool result = false;

        if (ImGui.BeginChildFrame(1, new Num.Vector2(400, 400)))
        {
            var di = CurrentFolder;
            if (di.Exists)
            {
                ImGui.BeginTable("Table", 4, ImGuiTableFlags.Sortable);

                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultSort, 0.0f, 1);
                ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Modified", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed);

                ImGui.TableHeadersRow();

                ImGui.TableNextRow();
                var parentDir = di.Parent;
                if (parentDir is not null && di.FullName != RootFolder)
                {
                    ImGui.TableSetColumnIndex(0);
                    ImGui.PushStyleColor(ImGuiCol.Text, (uint)System.Drawing.Color.Yellow.ToArgb());
                    {
                        if (ImGui.Selectable("../", false, ImGuiSelectableFlags.DontClosePopups))
                            CurrentFolder = parentDir;
                    }
                    ImGui.PopStyleColor();
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(string.Empty);
                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text(string.Empty);
                    ImGui.TableSetColumnIndex(3);
                    ImGui.Text(string.Empty);
                }

                // TODO: Table sorting, this is completely undocumented in C#
                // var sorts_specs = ImGui.TableGetSortSpecs();

                //TODO: Cache This
                var fileSystemEntries = GetFileSystemEntries(di);
                foreach (var fse in fileSystemEntries)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    if (fse.IsDirectory)
                    {
                        var name = fse.Name;

                        ImGui.PushStyleColor(ImGuiCol.Text, (uint)System.Drawing.Color.Yellow.ToArgb());
                        if (ImGui.Selectable(name + "/", false, ImGuiSelectableFlags.DontClosePopups))
                            CurrentFolder = new DirectoryInfo(Path.Combine(CurrentFolder.FullName, fse.Name));
                        ImGui.PopStyleColor();
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
                    }

                    string type;
                    if(fse.IsDirectory)
                        type = string.Empty;
                    else
                    {
                        var ext = Path.GetExtension(fse.Name);
                        if (!string.IsNullOrEmpty(ext))
                            type = ext.Substring(1).ToUpperInvariant();
                        else
                            type = fse.Name;
                    }

                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(type);
                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text(fse.LastModifiedUtc.ToLocalTime().ToShortDateString());
                    ImGui.TableSetColumnIndex(3);
                    ImGui.Text(fse.IsDirectory ? string.Empty : fse.Length.ToString());
                }

                ImGui.EndTable();
            }
        }
        ImGui.EndChildFrame();

        if (ImGui.Button("Cancel"))
        {
            result = false;
            ImGui.CloseCurrentPopup();
            return result;
        }

        var btnName = _dlgType == ComDlgType.FileSaveDialog ? "Save" : "Open";
        if (OnlyAllowFolders)
        {
            ImGui.SameLine();
            if (ImGui.Button(btnName))
            {
                result = true;
                FullPath = CurrentFolder.FullName;
                ImGui.CloseCurrentPopup();
            }
        }
        else
        {
            ImGui.SameLine();

            var savePath = Path.Combine(CurrentFolder.FullName, SelectedFile ?? string.Empty);
            ImGui.BeginDisabled(SelectedFile is null || Directory.Exists(savePath));
            if (ImGui.Button(btnName))
            {
                result = true;
                FullPath = Path.Combine(CurrentFolder.FullName, SelectedFile!);
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndDisabled();

            ImGui.SameLine();

            str = SelectedFile ?? string.Empty;
            ImGui.PushItemWidth(-1);
            if (ImGui.InputText("##FileTxt", ref str, maxPath, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if(_dlgType != ComDlgType.FolderSelectDialog)
                {
                    var file = new FileInfo(str);

                    if(_dlgType == ComDlgType.FileOpenDialog)
                    {
                        if (file.Exists)
                        {
                            result = true;
                            FullPath = file.FullName;
                            ImGui.CloseCurrentPopup();
                        }
                        else if (File.Exists(Path.Combine(CurrentFolder.FullName, str)))
                        {
                            result = true;
                            FullPath = Path.Combine(CurrentFolder.FullName, str);
                            ImGui.CloseCurrentPopup();
                        }
                    }
                    else if (_dlgType == ComDlgType.FileSaveDialog)
                    {
                        // can't save over folders
                        var filePath = Path.Combine(CurrentFolder.FullName, str);
                        if (!Directory.Exists(filePath))
                        {
                            result = true;
                            FullPath = filePath;
                            ImGui.CloseCurrentPopup();
                        }
                    }
                    else if (Directory.Exists(file.FullName))
                    {
                        CurrentFolder = new DirectoryInfo(file.FullName);
                    }
                }
                else
                {
                    if (Directory.Exists(str))
                    {
                        result = true;
                        FullPath = str;
                        ImGui.CloseCurrentPopup();
                    }
                }
            }
            ImGui.PopItemWidth();
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