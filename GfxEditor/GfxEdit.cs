namespace GfxEditor;

internal class GfxEdit
{
    public event EventHandler? FileUpdated;

    public FileInfo? OpenFileInfo { get; private set; }
    public File3di OpenedFile { get; private set; } = new();

    public int ActiveLod { get; set; }

    public void NewFile()
    {
        OpenFileInfo = null;
        OpenedFile = new File3di();
        FileDirty();
    }

    public void LoadFile(FileInfo fileInfo)
    {
        if (!fileInfo.Exists)
            return;

        OpenedFile = new File3di();

        OpenFileInfo = fileInfo;
        using var stream = fileInfo.OpenRead();
        using var reader = new BinaryReader(stream);
        OpenedFile.ReadFile(reader);
        FileDirty();
    }

    public void SaveFile(FileInfo fileInfo)
    {
        //TODO: Save file
    }

    public void FileDirty()
    {
        FileUpdated?.Invoke(this, EventArgs.Empty);
    }
}