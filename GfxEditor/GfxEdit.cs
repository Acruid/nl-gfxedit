namespace GfxEditor;

internal class GfxEdit
{
    public event EventHandler? FileUpdated;

    public File3di OpenedFile { get; private set; } = new();

    public void NewFile()
    {
        OpenedFile = new File3di();
        FileDirty();
    }

    public void LoadFile(FileInfo fileInfo)
    {
        //TODO: Load file

        OpenedFile = new File3di();
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