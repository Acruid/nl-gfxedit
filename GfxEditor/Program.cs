namespace GfxEditor;

internal static class Program
{
    private static void Main()
    {
        var editor = new GfxEdit();

        Window wnd = new Window(editor);
        wnd.Run();
    }
}