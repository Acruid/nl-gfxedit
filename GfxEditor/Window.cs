using System.Runtime.InteropServices;
using ImGuiNET;
using Nez.ImGuiTools;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace GfxEditor;

/// <summary>
/// The MDI root OpenGL window that contains the program UI.
/// </summary>
internal class Window : GameWindow
{
    private readonly GfxEdit _gfxEdit;
    ImGuiController _controller;
    SceneRender _scene;

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
        _scene = new SceneRender(this, new TriangleDrawer());
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
                var startingPath = @"D:\Installed Games\Delta Force 2";
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
