namespace GfxEditor;

internal static class Program
{
    private static int Main(string[] args)
    {
        var editor = new GfxEdit();

        if (args.Length == 1)
        {
            var file = new FileInfo(args[0]);
            if(file.Exists)
                editor.LoadFile(file);
        }

        Window wnd = new Window(editor);

        wnd.Run();

        return 0;
    }
}