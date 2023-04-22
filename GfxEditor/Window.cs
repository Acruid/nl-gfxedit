using System.Runtime.InteropServices;
using ImGuiNET;
using Nez.ImGuiTools;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using static GfxEditor.File3di;
using static OpenTK.Graphics.OpenGL.GL;

namespace GfxEditor;

/// <summary>
/// The MDI root OpenGL window that contains the program UI.
/// </summary>
internal class Window : GameWindow
{
    private readonly GfxEdit _gfxEdit;
    ImGuiController _controller;
    SceneRender _scene;
    ITriangleBatch _drawer;

    public Window(GfxEdit gfxEdit) : base(GameWindowSettings.Default, new NativeWindowSettings() { Size = new Vector2i(1600, 900), APIVersion = new Version(3, 3) })
    {
        _gfxEdit = gfxEdit;
    }

    private static DebugProc _debugProcCallback = DebugCallback;
    private static GCHandle _debugProcCallbackHandle;
    private static void DebugCallback(DebugSource source, DebugType type, int id,
    DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
    {
        string messageString = Marshal.PtrToStringAnsi(message, length);
        Console.WriteLine($"{severity} {type} | {messageString}");

        if (type == DebugType.DebugTypeError)
            throw new Exception(messageString);
    }

    void SetupDebugging()
    {
        _debugProcCallbackHandle = GCHandle.Alloc(_debugProcCallback);

        GL.DebugMessageCallback(_debugProcCallback, IntPtr.Zero);
        GL.Enable(EnableCap.DebugOutput);
        GL.Enable(EnableCap.DebugOutputSynchronous);
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        SetupDebugging();
        Title += ": OpenGL Version: " + GL.GetString(StringName.Version);
        VSync = VSyncMode.On;

        _controller = new ImGuiController(ClientSize.X, ClientSize.Y);

        var drawer = new TriangleDrawer();
        _drawer = drawer;
        _scene = new SceneRender(this, drawer);

        GlError.Check();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        // Update the opengl viewport
        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);

        // Tell ImGui of the new size
        _controller.WindowResized(ClientSize.X, ClientSize.Y);
    }

    private bool _showGameDirModal = false;
    private bool _showOpenGfxModal = false;

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        PushModelTriangles();

        _controller.Update(this, (float)e.Time);

        GL.ClearColor(new Color4(0, 32, 48, 255));
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

        // Start the menu bar.
        ImGui.BeginMainMenuBar();

        // Add a file menu.
        if (ImGui.BeginMenu("File"))
        {
            // Add a new item to the file menu.
            if (ImGui.MenuItem("New"))
            {
                // Do something.
            }

            if (ImGui.MenuItem("Open Game Directory..."))
            {
                _showGameDirModal = true;
            }

            if (ImGui.MenuItem("Open GFX 3DI..."))
            {
                _showOpenGfxModal = true;
            }

            // Add a save item to the file menu.
            if (ImGui.MenuItem("Save"))
            {
                // Do something.
            }

            // Add a exit item to the file menu.
            if (ImGui.MenuItem("Exit"))
            {
                Close();
            }

            // End the file menu.
            ImGui.EndMenu();
        }

        _controller.StartDockspace();

        GlError.Check();
        ImGui.ShowDemoWindow();

        GlError.Check();
        _scene.DrawViewportWindow();

        GlError.Check();
        _controller.EndDockspace();

        if (_showGameDirModal)
        {
            ImGui.OpenPopup("file-open-GameDir");

            if (ImGui.BeginPopupModal("file-open-GameDir", ref _showGameDirModal, ImGuiWindowFlags.NoTitleBar))
            {
                var picker = FilePicker.GetFolderPicker(this, Path.Combine(Environment.CurrentDirectory));
                if (picker.Draw())
                {
                    Console.WriteLine(picker.SelectedFile);
                    FilePicker.RemoveFilePicker(this);
                }
                ImGui.EndPopup();
            }

            if (!ImGui.IsPopupOpen("file-open-GameDir"))
                _showGameDirModal = false;
        }

        if (_showOpenGfxModal)
        {
            ImGui.OpenPopup("file-open-GfxModal");

            if (ImGui.BeginPopupModal("file-open-GfxModal", ref _showOpenGfxModal, ImGuiWindowFlags.NoTitleBar))
            {
                var startingPath = @"D:\Projects\DF2 - Delta Force 2\Delta Force 2";
                var picker = FilePicker.GetFilePicker(this, startingPath, ".3di");
                if (picker.Draw())
                {
                    Console.WriteLine(picker.SelectedFile);
                    _gfxEdit.LoadFile(new FileInfo(picker.SelectedFile));
                    FilePicker.RemoveFilePicker(this);
                }
                ImGui.EndPopup();
            }

            if (!ImGui.IsPopupOpen("file-open-GfxModal"))
                _showOpenGfxModal = false;
        }

        _controller.Render();

        ImGuiController.CheckGLError("End of frame");

        SwapBuffers();
    }

    private void PushModelTriangles()
    {
        // get all triangles from gfx active lod and push to TriangleBatch

        const CamoColor camo = CamoColor.Green;
        var gfx = _gfxEdit.OpenedFile;

        if (gfx is null || gfx._header.nLODs == 0) return;

        var lod = _gfxEdit.ActiveLod;

        for(var iBone = 0; iBone < gfx._lodSubObjects[lod].Length; iBone++)
        {
            var bone = gfx._lodSubObjects[lod][iBone];

            var foff = gfx.FaceOffset(lod, iBone); // offset into face array for bone
            var voff = gfx.VecOffset(lod, iBone); // offset into vertex array for bone

            for (var i = 0; i < bone.nFaces; i++)
            {
                var face = gfx._lodFaces[lod][i + foff];
                var material = gfx._lodMaterials[lod][face.MaterialIndex];
                var texture = gfx._textures[material.TexIndex(camo)];

                var boneOffset = new Vector4() { X = bone.VecXoff >> 8, Y = bone.VecYoff >> 8, Z = bone.VecZoff >> 8 };

                {
                    //TODO: Make the face verts arrays
                    var vertPos = gfx._lodPositions[lod][face.Vertex1 + voff];
                    var tkVPos = new Vector4(vertPos.x, vertPos.y, vertPos.z, vertPos.w);
                    var pos = (tkVPos - boneOffset).Xyz / 256;
                    var vert = new TriangleDrawer.Vertex(pos, OpenTK.Mathematics.Color4.Red);
                    _drawer.Append(in vert);

                    vertPos = gfx._lodPositions[lod][face.Vertex2 + voff];
                    tkVPos = new Vector4(vertPos.x, vertPos.y, vertPos.z, vertPos.w);
                    pos = (tkVPos - boneOffset).Xyz / 256;
                    vert = new TriangleDrawer.Vertex(pos, OpenTK.Mathematics.Color4.Red);
                    _drawer.Append(in vert);

                    vertPos = gfx._lodPositions[lod][face.Vertex3 + voff];
                    tkVPos = new Vector4(vertPos.x, vertPos.y, vertPos.z, vertPos.w);
                    pos = (tkVPos - boneOffset).Xyz / 256;
                    vert = new TriangleDrawer.Vertex(pos, OpenTK.Mathematics.Color4.Red);
                    _drawer.Append(in vert);
                }
            }
        }
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);


        _controller.PressChar((char)e.Unicode);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        _controller.MouseScroll(e.Offset);
    }

    protected override void OnUnload()
    {
        _scene.Dispose();
        _controller.Dispose();

        base.OnUnload();
    }
}
