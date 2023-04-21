using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace GfxEditor;

public interface ITriangleBatch
{
    void Append(in TriangleDrawer.Vertex vertex);
    void Clear();
}

public class TriangleDrawer : IModelDrawer, ITriangleBatch
{
    private const int BatchSize = 512 * 3; // 512 triangles

    private int _width;
    private int _height;

    private int _vertexBufferObject;
    private int _vertexArrayObject;
    private Shader _shader;

    private int _numVertices;
    private readonly Vertex[] _vertices = new Vertex[BatchSize];

    /*
    private Vertex[] _vertices =
    {
        // Opaque triangle
        new Vertex(new Vector3(-0.5f, 0.5f, 0.0f), new Color4(1.0f, 0.0f, 0.0f, 128/255.0f)),
        new Vertex(new Vector3(-1.0f, -0.5f, 0.0f), new Color4(1.0f, 0.0f, 0.0f, 128/255.0f)),
        new Vertex(new Vector3(0.0f, -0.5f, 0.0f), new Color4(1.0f, 0.0f, 0.0f, 128/255.0f)),

        // First transparent triangle
        new Vertex(new Vector3(0.5f, 0.5f, 0.0f), new Color4(0.0f, 1.0f, 0.0f, 128/255.0f)),
        new Vertex(new Vector3(1.0f, -0.5f, 0.0f), new Color4(0.0f, 1.0f, 0.0f, 128/255.0f)),
        new Vertex(new Vector3(0.0f, -0.5f, 0.0f), new Color4(0.0f, 1.0f, 0.0f, 128/255.0f)),

        // Second transparent triangle
        new Vertex(new Vector3(0.0f, 1.0f, 0.0f), new Color4(0.0f, 0.0f, 1.0f, 128/255.0f)),
        new Vertex(new Vector3(-0.5f, 0.5f, 0.0f), new Color4(0.0f, 0.0f, 1.0f, 128/255.0f)),
        new Vertex(new Vector3(0.5f, 0.5f, 0.0f), new Color4(0.0f, 0.0f, 1.0f, 128/255.0f))
    };
    */

    public void OnLoad()
    {
        // Create Vertex Buffer Object
        _vertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * Marshal.SizeOf<Vertex>(), IntPtr.Zero, BufferUsageHint.StreamDraw);

        // Create Vertex Array Object
        _vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArrayObject);

        // Set up Vertex Attribute Pointers
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 7 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 7 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        // Create and use Shader
        string vertexShaderSource = @"
    #version 330 core

    layout (location = 0) in vec3 aPosition;
    layout (location = 1) in vec4 aColor;

    out vec4 vColor;

    void main()
    {
        gl_Position = vec4(aPosition, 1.0);
        vColor = aColor;
    }
";

        string fragmentShaderSource = @"
    #version 330 core

    in vec4 vColor;

    out vec4 FragColor;

    void main()
    {
        FragColor = vColor;
    }
";

        _shader = new Shader(vertexShaderSource, fragmentShaderSource);
        _shader.Use();
    }

    public void OnUnload()
    {
        // Clean up resources
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.DeleteBuffer(_vertexBufferObject);
        GL.BindVertexArray(0);
        GL.DeleteVertexArray(_vertexArrayObject);
        _shader.Dispose();
    }

    public void OnResize(int width, int height)
    {
        _width = width;
        _height = height;

        GL.Viewport(0, 0, _width, _height);
    }

    public void OnRenderFrame()
    {
        // Set clear color
        GL.ClearColor(Color4.Black);

        // set the winding mode to clockwise
        GL.FrontFace(FrontFaceDirection.Cw);

        // Clear the screen
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Update vertex data
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * Marshal.SizeOf<Vertex>(), IntPtr.Zero, BufferUsageHint.StreamDraw);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _vertices.Length * Marshal.SizeOf<Vertex>(), _vertices);

        // Enable blending
        GL.Enable(EnableCap.Blend);
        //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);

        // Bind the Vertex Array Object and draw the triangles
        GL.BindVertexArray(_vertexArrayObject);
        GL.DrawArrays(PrimitiveType.Triangles, 0, _numVertices);
    }

    public struct Vertex
    {
        public readonly Vector3 Position;
        public readonly Color4 Color;

        public Vertex(Vector3 position, Color4 color)
        {
            Position = position;
            Color = color;
        }
    }

    public void Append(in Vertex vertex)
    {
        _vertices[_numVertices] = vertex;
        _numVertices++;
    }

    public void Clear()
    {
        _numVertices = 0;
    }
}

public class Shader : IDisposable
{
    public readonly int Handle;

    public Shader(string vertexShaderSource, string fragmentShaderSource)
    {
        // Create and compile the vertex shader
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.CompileShader(vertexShader);

        // Check for compilation errors
        string infoLog = GL.GetShaderInfoLog(vertexShader);
        if (!string.IsNullOrWhiteSpace(infoLog))
            throw new Exception($"Error compiling vertex shader: {infoLog}");

        // Create and compile the fragment shader
        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        GL.CompileShader(fragmentShader);

        // Check for compilation errors
        infoLog = GL.GetShaderInfoLog(fragmentShader);
        if (!string.IsNullOrWhiteSpace(infoLog))
            throw new Exception($"Error compiling fragment shader: {infoLog}");

        // Create and link the shader program
        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vertexShader);
        GL.AttachShader(Handle, fragmentShader);
        GL.LinkProgram(Handle);

        // Check for linking errors
        infoLog = GL.GetProgramInfoLog(Handle);
        if (!string.IsNullOrWhiteSpace(infoLog))
            throw new Exception($"Error linking shader program: {infoLog}");

        // Clean up the shaders
        GL.DetachShader(Handle, vertexShader);
        GL.DetachShader(Handle, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }

    public void Use()
    {
        GL.UseProgram(Handle);
    }

    public void Dispose()
    {
        GL.DeleteProgram(Handle);
    }
}