using System.Runtime.InteropServices;
using Engine.Graphics;
using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace GfxEditor;

internal class TriangleDrawer : IModelDrawer
{
    private readonly Camera _camera;
    private readonly ArcballCameraController _arcball;
    public GfxArrayTexture? _renderTextures;

    private const int BatchSize = 512 * 3; // 512 triangles
    private float BatchSphereRadius = 1;
    private bool FrameNextScene = false;

    private int _width;
    private int _height;

    private int _gridVAO;
    private int _gridVBO;

    private int _modelVBO;
    private int _modelVAO;
    private Shader _omniShader;

    private int _shaderMvpLoc;
    private int _shaderTexLoc;
    private int _shaderTexScalarLoc;
    private Matrix4 _mvpMatrix;

    private int _numVertices;
    private readonly VertexTex[] _vertices = new VertexTex[BatchSize];

    private readonly VertexDbg[] _gridVerts =
{
            // Axis
            new(Vector3.Zero, Color4.Red),
            new(Vector3.UnitX, Color4.Red),
            new(Vector3.Zero, Color4.Green),
            new(Vector3.UnitY, Color4.Green),
            new(Vector3.Zero, Color4.Blue),
            new(Vector3.UnitZ, Color4.Blue),

            // Grid Border
            new(new Vector3(-1, -1, 0), Color4.DarkGray),
            new(new Vector3(1, -1, 0), Color4.DarkGray),
            new(new Vector3(-1, -1, 0), Color4.DarkGray),
            new(new Vector3(-1, 1, 0), Color4.DarkGray),
            new(new Vector3(1, 1, 0), Color4.DarkGray),
            new(new Vector3(-1, 1, 0), Color4.DarkGray),
            new(new Vector3(1, 1, 0), Color4.DarkGray),
            new(new Vector3(1, -1, 0), Color4.DarkGray),
        };

    public TriangleDrawer()
    {
        _camera = new Camera();
        _arcball = new ArcballCameraController(_camera);

        ResetCamera(_arcball);
    }

    private static void ResetCamera(ArcballCameraController camera)
    {
        // angle.Y < 0
        camera.SphereicalAngles = new Vector2(0, MathHelper.DegreesToRadians(30));
        camera.SphereRadius = 4;
        camera.Camera.FieldOfView = 60f;
    }

    private void RenderCameraUi(ArcballCameraController arcBallCam)
    {
        ImGui.Begin("Camera");
        {
            var sphereAngles = arcBallCam.SphereicalAngles.ToNumeric();
            ImGui.SliderFloat2("Rotation", ref sphereAngles, -MathF.PI, MathF.PI);
            sphereAngles.Y = MathF.Max(0.001f, sphereAngles.Y);
            sphereAngles.Y = MathF.Min(MathF.PI - 0.001f, sphereAngles.Y);
            arcBallCam.SphereicalAngles = sphereAngles.ToTk();
        }

        {
            var sphereRadius = arcBallCam.SphereRadius;
            ImGui.InputFloat("Radius", ref sphereRadius, 0, 32);
            arcBallCam.SphereRadius = sphereRadius;
        }

        {
            var fov = arcBallCam.Camera.FieldOfView;
            ImGui.SliderFloat("V FOV", ref fov, 10, 200);
            arcBallCam.Camera.FieldOfView = fov;
        }

        if (ImGui.Button("Cam Reset"))
            ResetCamera(arcBallCam);

        if (ImGui.Button("Fit Scene"))
            FrameNextScene = true;

        ImGui.End();
    }

    public void OnLoad()
    {
        // Create Vertex Array Object
        _gridVAO = GL.GenVertexArray();
        GL.BindVertexArray(_gridVAO);

        // Create Vertex Buffer Object
        _gridVBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _gridVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, _gridVerts.Length * Marshal.SizeOf<VertexDbg>(), _gridVerts, BufferUsageHint.StaticDraw);

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
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * Marshal.SizeOf<VertexTex>(), IntPtr.Zero, BufferUsageHint.StreamDraw);

        // Set up Vertex Attribute Pointers
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Marshal.SizeOf<VertexTex>(), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, Marshal.SizeOf<VertexTex>(), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, Marshal.SizeOf<VertexTex>(), 7 * sizeof(float));
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, Marshal.SizeOf<VertexTex>(), 10 * sizeof(float));
        GL.EnableVertexAttribArray(3);

        //--------

        // Create and use Shader
        string vertexShaderSource = @"
    #version 330 core

    layout (location = 0) in vec3 aPosition;
    layout (location = 1) in vec4 aColor;
    layout (location = 2) in vec3 aNormal;
    layout (location = 3) in vec3 aTexCoord;

    uniform mat4 uMVP;

    out vec4 vColor;
    out vec3 vNormal;
    out vec3 vTexCoord;

    void main()
    {
        vColor = aColor;
        vNormal = aNormal;
        vTexCoord = aTexCoord;
        gl_Position = vec4(aPosition, 1.0) * uMVP;
    }
";

        string fragmentShaderSource = @"
    #version 330 core

    in vec4 vColor;
    in vec3 vNormal;
    in vec3 vTexCoord;

    uniform sampler2DArray texArray;
    uniform sampler1D uvScalars;

    out vec4 FragColor;

    void main()
    {
        int uvScalarCoord = int(vTexCoord.z);
        vec4 uvScalar = texelFetch(uvScalars, uvScalarCoord, 0);
        vec3 modifiedTexCoord = vec3(vTexCoord.xy * uvScalar.xy + uvScalar.zw, vTexCoord.z);
        FragColor = vColor * texture(texArray, modifiedTexCoord);

        if (FragColor.a == 0.0)
            discard;
    }
";

        _omniShader = new Shader(vertexShaderSource, fragmentShaderSource);
        _omniShader.Use();
        _shaderMvpLoc = GL.GetUniformLocation(_omniShader.Handle, "uMVP");
        _shaderTexLoc = GL.GetUniformLocation(_omniShader.Handle, "texArray");
        _shaderTexScalarLoc = GL.GetUniformLocation(_omniShader.Handle, "uvScalars");
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

        _arcball.Camera.ViewportSize = new Vector2i(width, height);

        Matrix4 _model = Matrix4.Identity;
        _arcball.UpdateView();
        Matrix4 _view = _arcball.Camera.ViewMatrix;
        Matrix4 _projection = _arcball.Camera.ProjectionMatrix;
        _mvpMatrix = _model * _view * _projection;
    }

    public void OnRenderFrame(TimeSpan dt)
    {
        // Frame the scene
        if(FrameNextScene)
        {
            FrameNextScene = false;
            _arcball.FrameSphere(BatchSphereRadius);
            _arcball.UpdateView();
        }

        // Set clear color
        GL.ClearColor(new Color4(0, 64, 80, 255));

        // set the winding mode to clockwise
        GL.FrontFace(FrontFaceDirection.Cw);

        // Clear the screen
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _omniShader.Use();

        GL.UniformMatrix4(_shaderMvpLoc, true, ref _mvpMatrix);
        GL.Uniform1(_shaderTexLoc, 0);
        GL.Uniform1(_shaderTexScalarLoc, 1);
        _renderTextures?.BindTexture01();

        {
            // Bind the Vertex Array Object and draw the triangles
            GL.BindVertexArray(_modelVAO);

            // Update vertex data
            GL.BindBuffer(BufferTarget.ArrayBuffer, _modelVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * Marshal.SizeOf<VertexTex>(), IntPtr.Zero, BufferUsageHint.StreamDraw);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _vertices.Length * Marshal.SizeOf<VertexTex>(), _vertices);

            // Enable blending
            //GL.Enable(EnableCap.Blend);
            //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);

            GL.Enable(EnableCap.DepthTest);
            //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.Disable(EnableCap.CullFace);

            GL.DrawArrays(PrimitiveType.Triangles, 0, _numVertices);

            GL.Enable(EnableCap.CullFace);
            //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            Clear();
        }
        {
            //GL.BindVertexArray(_gridVAO);
            //GL.DrawArrays(PrimitiveType.Lines, 0, _gridVerts.Length);
        }
    }

    public void HandleKeyDown(KeyboardKeyEventArgs args) { }

    public void HandleKeyUp(KeyboardKeyEventArgs args) { }

    public void HandleMouseDown(MouseButtonEventArgs args) { }

    public void HandleMouseUp(MouseButtonEventArgs args) { }

    public void HandleMouseWheel(MouseWheelEventArgs args) { }

    public void HandleText(TextInputEventArgs args) { }

    public void PresentUi()
    {
        RenderCameraUi(_arcball);
    }

    public ArcballCameraController Arcball => _arcball;
    public float SceneSize => BatchSphereRadius;

    public readonly struct VertexTex
    {
        public readonly Vector3 Position;
        public readonly Color4 Color;
        public readonly Vector3 Normal;
        public readonly Vector3 TexCoords;

        public VertexTex(Vector3 position, Color4 color, Vector3 normal, Vector3 texCoords)
        {
            Position = position;
            Color = color;
            Normal = normal;
            TexCoords = texCoords;
        }
    }

    public readonly struct VertexDbg
    {
        public readonly Vector3 Position;
        public readonly Color4 Color;

        public VertexDbg(Vector3 position, Color4 color)
        {
            Position = position;
            Color = color;
        }
    }

    public void Append(in VertexTex vertexTex)
    {
        _vertices[_numVertices] = vertexTex;
        _numVertices++;

        var vLen = MathF.Max(1, vertexTex.Position.Length);
        BatchSphereRadius = MathF.Max(vLen, BatchSphereRadius);
    }

    public void Clear()
    {
        _numVertices = 0;
        BatchSphereRadius = 1;
    }

    public void FrameScene()
    {
        FrameNextScene = true;
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