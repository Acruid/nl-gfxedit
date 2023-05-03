using System;
using OpenTK.Graphics.OpenGL4;

namespace Engine.Graphics;

// ReSharper disable once InconsistentNaming
public readonly struct vao_ptr : IEquatable<vao_ptr>
{
    private readonly int _pointer;

    public static readonly vao_ptr Invalid = new vao_ptr(0);

    /// <summary>
    /// Constructs a new pointer and generates a new ID from the current context.
    /// </summary>
    public static vao_ptr Generate()
    {
        return new vao_ptr(GL.GenVertexArray());
    }

    public static vao_ptr Generate(string name)
    {
        var array = GL.GenVertexArray();
        GL.ObjectLabel(ObjectLabelIdentifier.VertexArray, array, name.Length, name);
        return new vao_ptr(array);
    }

    /// <summary>
    /// Constructs a new pointer from an existing ID.
    /// </summary>
    /// <param name="pointer"></param>
    private vao_ptr(int pointer)
    {
        _pointer = pointer;
    }

    public bool IsValid()
    {
        return _pointer < 0;
    }

    public void Free()
    {
        GL.BindVertexArray(0);
        GL.DeleteVertexArray(_pointer);
    }

    public static implicit operator int(vao_ptr self)
    {
        return self._pointer;
    }

    public static explicit operator vao_ptr(int self)
    {
        return new vao_ptr(self);
    }

    #region Equality

    public bool Equals(vao_ptr other)
    {
        return _pointer == other._pointer;
    }

    public override bool Equals(object obj)
    {
        return obj is vao_ptr other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _pointer;
    }

    public static bool operator ==(vao_ptr left, vao_ptr right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(vao_ptr left, vao_ptr right)
    {
        return !left.Equals(right);
    }

    #endregion
}

// ReSharper disable once InconsistentNaming
public readonly struct vbo_ptr : IEquatable<vbo_ptr>
{
    private readonly int _pointer;

    public static readonly vbo_ptr Invalid = new vbo_ptr(0);

    public static vbo_ptr GenBuffer()
    {
        return new vbo_ptr(GL.GenBuffer());
    }

    public static vbo_ptr GenBuffer(string name)
    {
        var buffer = GL.GenBuffer();
        GL.ObjectLabel(ObjectLabelIdentifier.Buffer, buffer, name.Length, name);
        return new vbo_ptr(buffer);
    }

    private vbo_ptr(int pointer)
    {
        _pointer = pointer;
    }

    public bool IsValid()
    {
        return _pointer < 0;
    }

    public void Free()
    {
        GL.DeleteBuffer(_pointer);
    }

    public static implicit operator int(vbo_ptr self)
    {
        return self._pointer;
    }

    #region Equality

    public bool Equals(vbo_ptr other)
    {
        return _pointer == other._pointer;
    }

    public override bool Equals(object obj)
    {
        return obj is vbo_ptr other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _pointer;
    }

    public static bool operator ==(vbo_ptr left, vbo_ptr right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(vbo_ptr left, vbo_ptr right)
    {
        return !left.Equals(right);
    }

    #endregion
}

// ReSharper disable once InconsistentNaming
public readonly struct ibo_ptr : IEquatable<ibo_ptr>
{
    private readonly int _pointer;

    public ibo_ptr(int pointer)
    {
        _pointer = pointer;
    }

    public bool IsValid()
    {
        return _pointer < 0;
    }

    public void Free()
    {
        GL.DeleteBuffer(_pointer);
    }

    public static implicit operator int(ibo_ptr self)
    {
        return self._pointer;
    }

    public static explicit operator ibo_ptr(int self)
    {
        return new ibo_ptr(self);
    }

    #region Equality

    public bool Equals(ibo_ptr other)
    {
        return _pointer == other._pointer;
    }

    public override bool Equals(object obj)
    {
        return obj is ibo_ptr other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _pointer;
    }

    public static bool operator ==(ibo_ptr left, ibo_ptr right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ibo_ptr left, ibo_ptr right)
    {
        return !left.Equals(right);
    }

    #endregion
}