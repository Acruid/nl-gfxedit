using OpenTK.Graphics.OpenGL;

namespace GfxEditor.Graphics;

internal static class GlError
{
    public static void Check()
    {
        ErrorCode errorCode = GL.GetError();
        if (errorCode != ErrorCode.NoError)
        {
            throw new InvalidOperationException();
        }
    }
}