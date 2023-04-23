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

    private int _gridVAO;
    private int _gridVBO;

    private int _modelVBO;
    private int _modelVAO;
    private Shader _omniShader;

    private int _shaderMvpLoc;
    private Matrix4 _mvpMatrix;

    private int _numVertices;
    private readonly Vertex[] _vertices = new Vertex[BatchSize];

    private readonly Vertex[] _gridVerts =
{
            // Axis
            new Vertex(Vector3.Zero, Color4.Red),
            new Vertex(Vector3.UnitX, Color4.Red),
            new Vertex(Vector3.Zero, Color4.Green),
            new Vertex(Vector3.UnitY, Color4.Green),
            new Vertex(Vector3.Zero, Color4.Blue),
            new Vertex(Vector3.UnitZ, Color4.Blue),

            // Grid Border
            new Vertex(new Vector3(-1, -1, 0), Color4.DarkGray),
            new Vertex(new Vector3(1, -1, 0), Color4.DarkGray),
            new Vertex(new Vector3(-1, -1, 0), Color4.DarkGray),
            new Vertex(new Vector3(-1, 1, 0), Color4.DarkGray),
            new Vertex(new Vector3(1, 1, 0), Color4.DarkGray),
            new Vertex(new Vector3(-1, 1, 0), Color4.DarkGray),
            new Vertex(new Vector3(1, 1, 0), Color4.DarkGray),
            new Vertex(new Vector3(1, -1, 0), Color4.DarkGray),
        };

    public void OnLoad()
    {
        // Create Vertex Array Object
        _gridVAO = GL.GenVertexArray();
        GL.BindVertexArray(_gridVAO);

        // Create Vertex Buffer Object
        _gridVBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _gridVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, _gridVerts.Length * Marshal.SizeOf<Vertex>(), _gridVerts, BufferUsageHint.StaticDraw);

        // Set up Vertex Attribute Pointers
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 7 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 7 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        //--------

        // Create Vertex Array Object
        _modelVAO = GL.GenVertexArray();
        GL.BindVertexArray(_modelVAO);

        // Create Vertex Buffer Object
        _modelVBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _modelVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * Marshal.SizeOf<Vertex>(), IntPtr.Zero, BufferUsageHint.StreamDraw);

        // Set up Vertex Attribute Pointers
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 7 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 7 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        //--------

        // Create and use Shader
        string vertexShaderSource = @"
    #version 330 core

    layout (location = 0) in vec3 aPosition;
    layout (location = 1) in vec4 aColor;

    uniform mat4 uMVP;

    out vec4 vColor;

    void main()
    {
        vColor = aColor;
        gl_Position = vec4(aPosition, 1.0) * uMVP;
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

        _omniShader = new Shader(vertexShaderSource, fragmentShaderSource);
        _omniShader.Use();
        _shaderMvpLoc = GL.GetUniformLocation(_omniShader.Handle, "uMVP");
    }

    public void OnUnload()
    {
        // Clean up resources
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.DeleteBuffer(_modelVBO);
        GL.BindVertexArray(0);
        GL.DeleteVertexArray(_modelVAO);
        _omniShader.Dispose();
    }

    public void OnResize(int width, int height)
    {
        _width = width;
        _height = height;

        GL.Viewport(0, 0, _width, _height);

        float aspectRatio = (float)width / height;
        Matrix4 _model = Matrix4.Identity;
        Matrix4 _view = Matrix4.LookAt(new Vector3(-4, 0, 1), new Vector3(0, 0, 0), new Vector3(0, 0, 1));
        Matrix4 _projection = Matrix4.CreatePerspectiveFieldOfView(60 * MathF.PI / 180.0f, aspectRatio, 0.1f, 100.0f);
        _mvpMatrix = _model * _view * _projection;
    }

    public void OnRenderFrame()
    {
        // Set clear color
        GL.ClearColor(new Color4(0, 64, 80, 255));

        // set the winding mode to clockwise
        GL.FrontFace(FrontFaceDirection.Cw);

        // Clear the screen
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _omniShader.Use();

        GL.UniformMatrix4(_shaderMvpLoc, true, ref _mvpMatrix);

        {
            // Bind the Vertex Array Object and draw the triangles
            GL.BindVertexArray(_modelVAO);

            // Update vertex data
            GL.BindBuffer(BufferTarget.ArrayBuffer, _modelVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * Marshal.SizeOf<Vertex>(), IntPtr.Zero, BufferUsageHint.StreamDraw);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _vertices.Length * Marshal.SizeOf<Vertex>(), _vertices);

            // Enable blending
            //GL.Enable(EnableCap.Blend);
            //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);

            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.Disable(EnableCap.CullFace);

            GL.DrawArrays(PrimitiveType.Triangles, 0, _numVertices);

            GL.Enable(EnableCap.CullFace);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            Clear();
        }
        {
            GL.BindVertexArray(_gridVAO);
            GL.DrawArrays(PrimitiveType.Lines, 0, _gridVerts.Length);
        }
    }

    public readonly struct Vertex
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