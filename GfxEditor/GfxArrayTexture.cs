using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace GfxEditor;

public readonly struct texture_ptr : IEquatable<texture_ptr>
{
    public readonly int Value;

    public texture_ptr(int value) => Value = value;
    public static implicit operator int(texture_ptr ptr) => ptr.Value;

    #region Equality
    public bool Equals(texture_ptr other) => Value == other.Value;
    public override bool Equals(object obj) => obj is texture_ptr other && Equals(other);
    public override int GetHashCode() => Value;
    public static bool operator ==(texture_ptr left, texture_ptr right) => left.Equals(right);
    public static bool operator !=(texture_ptr left, texture_ptr right) => !left.Equals(right);
    #endregion
}

internal class GfxArrayTexture : IDisposable
{
    // https://www.khronos.org/opengl/wiki/Array_Texture

    private texture_ptr texName;
    private Vector2i texSize;
    private Vector2[] texCoordScalar;

    public GfxArrayTexture(int width, int height, int layerCount)
    {
        texSize = new Vector2i(width, height);

        // Allocate space for the coordinate modifier
        texCoordScalar = new Vector2[layerCount];

        // generate a new texture name
        var texture = GL.GenTexture();
        texName = new texture_ptr(texture);

        // binds the texture to the 2d array target, and causes the texture to become a 2d array
        GL.BindTexture(TextureTarget.Texture2DArray, texture);

        // Allocate the storage for the array target.
        GL.TexStorage3D(TextureTarget3d.Texture2DArray, 1, SizedInternalFormat.Rgba8, width, height, layerCount);

        // Always set reasonable texture parameters
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
    }

    public void UploadTexture(int width, int height, int layer, byte[] texelsRgba)
    {
        // calculate subImage tex coord modifier
        var layerSize = new Vector2(width, height);
        texCoordScalar[layer] = layerSize / texSize;

        // copy the texel data to the CPU
        GL.TexSubImage3D(TextureTarget.Texture2DArray, 0, 0, 0, layer, width, height, 1, PixelFormat.Rgba, PixelType.UnsignedByte, texelsRgba);
    }

    public void BindTexture0()
    {
        // Select texture unit 0
        GL.ActiveTexture(TextureUnit.Texture0);

        // Bind the texture array to the currently active texture unit
        GL.BindTexture(TextureTarget.Texture2DArray, texName);
    }

    private void ReleaseUnmanagedResources()
    {
        // TODO release unmanaged resources here
        GL.DeleteTexture(texName);
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~GfxArrayTexture() {
        ReleaseUnmanagedResources();
    }

    public Vector3 ScaleCoords(Vector2 texCoords, byte texIndex)
    {
        return new Vector3(texCoords.X, texCoords.Y, texIndex);
    }
}