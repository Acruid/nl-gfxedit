using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using Engine.ImGuiBindings;
using ImGuiNET;
using Nez.ImGuiTools;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using static GfxEditor.File3di;
using static OpenTK.Graphics.OpenGL.GL;
using NVec2 = System.Numerics.Vector2;

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

        _gfxEdit.FileUpdated += (sender, args) => GfxFileUpdated();
    }

    private void GfxFileUpdated()
    {
        //TODO: Make me work!
        /*
        _drawer.ClearTextures();

        if (_gfxEdit.OpenedFile is null || _gfxEdit.OpenedFile._textures.Count == 0)
            return;

        for(var iTex = 0; iTex < _gfxEdit.OpenedFile._textures.Count; iTex++)
        {

        }
        */
    }

    private const string GuiTextureWindowClass = "Gfx Textures";

    int GfxTextureWindow_SelectedTexIdx = 0;
    int GfxTextureWindow_TexColorHandle = -1;
    int GfxTextureWindow_TexAlphaHandle = -1;
    int GfxTextureWindow_TexPalHandle = -1;
    private void PresentGfxTextureWindow()
    {
        if (!(_gfxEdit is not null && _gfxEdit.OpenedFile is not null && _gfxEdit.OpenedFile._textures is not null))
            return;

        List<TEXTURE> _textures = _gfxEdit.OpenedFile._textures;
        var texDirty = false;

        // prevents selected from going out of bounds when _textures count changes.
        if (GfxTextureWindow_SelectedTexIdx > _textures.Count)
        {
            GfxTextureWindow_SelectedTexIdx = 0;
            texDirty = true;
        }

        ImGui.Begin(GuiTextureWindowClass);
        ImGui.Text("Textures:");

        if (ImGui.BeginListBox("##UNIQUE_LBL_1", new NVec2(-1, 5 * ImGui.GetTextLineHeightWithSpacing())))
        {
            for (int iTex = 0; iTex < _textures.Count; iTex++)
            {
                bool is_selected = (GfxTextureWindow_SelectedTexIdx == iTex);
                if (ImGui.Selectable($"[{iTex}:{_textures[iTex].TexIndex}] {_textures[iTex].Name}", is_selected))
                {
                    if (GfxTextureWindow_SelectedTexIdx != iTex) texDirty = true;
                    GfxTextureWindow_SelectedTexIdx = iTex;
                }

                // Set the initial focus when opening the combo (scrolling + keyboard navigation focus)
                if (is_selected)
                    ImGui.SetItemDefaultFocus();
            }

            ImGui.EndListBox();
        }

        if (ImGui.Button("Import")) { }

        ImGui.SameLine();

        if (_textures.Count == 0) ImGui.BeginDisabled();

        if (ImGui.Button("Export")) { }

        ImGui.SameLine();

        if (ImGui.Button("Export All")) { }

        if (_textures.Count == 0) ImGui.EndDisabled();

        if (_textures.Count == 0)
        {
            ImGui.Text("Selected: ---");
        }
        else
        {
            var n = GfxTextureWindow_SelectedTexIdx;
            TEXTURE texture = _textures[n];
            ImGui.Text($"Selected: [{n}:{texture.TexIndex}] {texture.Name}");

            ImGui.Separator();
            // Name Edit
            texture.Name = ImGuiEx.InputString("Name", texture.Name) ?? string.Empty;

            // Flags Edit
            ImGui.Text("Flags:");
            ImGui.Separator();

            // ImGui::CheckboxFlags("io.ConfigFlags: NavEnableKeyboard",    &io.ConfigFlags, ImGuiConfigFlags_NavEnableKeyboard);
            // ImGui.CheckboxFlags()
            ImGui.Text($"TODO: {texture.Flags.ToString("X")} | {Convert.ToString(texture.Flags, 2)}");

            _textures[n] = texture; // write any changes back to the file
            ImGui.Separator();

            var texColorHandle = GfxTextureWindow_TexColorHandle;
            var texAlphaHandle = GfxTextureWindow_TexAlphaHandle;
            var texPalHandle = GfxTextureWindow_TexPalHandle;

            if (texColorHandle == -1 || texDirty)
            {
                if(texColorHandle != -1)
                {
                    GL.DeleteTexture(texColorHandle);
                    GfxTextureWindow_TexColorHandle = texColorHandle = -1;
                }

                if (texAlphaHandle != -1)
                {
                    GL.DeleteTexture(texAlphaHandle);
                    GfxTextureWindow_TexAlphaHandle = texAlphaHandle = -1;
                }

                if (texPalHandle != -1)
                {
                    GL.DeleteTexture(texPalHandle);
                    GfxTextureWindow_TexPalHandle = texPalHandle = -1;
                }

                var numPixels = texture.bmWidth * texture.bmHeight;
                var stride = texture.bmSize / numPixels;

                var texColorPixels = new byte[numPixels * 3];
                var texColor = texColorPixels.AsSpan();
                var texAlphaPixels = new byte[numPixels * 3];
                var texAlpha = texAlphaPixels.AsSpan();
                var texPalPixels = new byte[256 * 3];
                var texPal = texPalPixels.AsSpan();

                var bmLines = _gfxEdit.OpenedFile._bmLines[n].AsSpan();
                var palette = _gfxEdit.OpenedFile._palettes[n].AsSpan().AsBytes();

                //WARNING: This only works with powers of 2 sized images
                for (var i = 0; i < numPixels; i++)
                {
                    // GL: [RGB] 3DI Palette: [BGRA]

                    var index = bmLines[i * stride + 0] * 4;
                    //pixels[i * 4 + 3] = stride == 2 ? scanLines[i * stride + 1] : (byte)255; // A
                    texColor[i * 3 + 2] = palette[index + 0];
                    texColor[i * 3 + 1] = palette[index + 1];
                    texColor[i * 3 + 0] = palette[index + 2];

                    var alpha = stride == 2 ? bmLines[i * stride + 1] : (byte)255; // A
                    texAlpha[i * 3 + 2] = alpha;
                    texAlpha[i * 3 + 1] = alpha;
                    texAlpha[i * 3 + 0] = alpha;
                }

                for(var i = 0; i < 256; i++)
                {
                    // palette alpha is unused afaik
                    texPal[i * 3 + 2] = palette[i * 4 + 0];
                    texPal[i * 3 + 1] = palette[i * 4 + 1];
                    texPal[i * 3 + 0] = palette[i * 4 + 2];
                }

                GfxTextureWindow_TexColorHandle = texColorHandle = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, texColorHandle);
                GL.ObjectLabel(ObjectLabelIdentifier.Texture, texColorHandle, 12, "GfxWnd:Color");
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, texture.bmWidth, texture.bmHeight, 0, PixelFormat.Rgb, PixelType.UnsignedByte, texColorPixels);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

                GfxTextureWindow_TexAlphaHandle = texAlphaHandle = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, texAlphaHandle);
                GL.ObjectLabel(ObjectLabelIdentifier.Texture, texAlphaHandle, 12, "GfxWnd:Alpha");
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, texture.bmWidth, texture.bmHeight, 0, PixelFormat.Rgb, PixelType.UnsignedByte, texAlphaPixels);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

                GfxTextureWindow_TexPalHandle = texPalHandle = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, texPalHandle);
                GL.ObjectLabel(ObjectLabelIdentifier.Texture, texPalHandle, 14, "GfxWnd:Palette");
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, 16, 16, 0, PixelFormat.Rgb, PixelType.UnsignedByte, texPalPixels);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            }

            // Color Image
            if (texColorHandle != -1)
            {
                ImGui.Image(new IntPtr(texColorHandle), new NVec2(texture.bmWidth, texture.bmHeight));
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.DelayShort))
                {
                    ImGui.BeginTooltip();
                    ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
                    ImGui.TextUnformatted("Color");
                    ImGui.PopTextWrapPos();
                    ImGui.EndTooltip();
                }
            }

            // Optional Alpha Mask Image
            if (texAlphaHandle != -1)
            {
                ImGui.Image(new IntPtr(texAlphaHandle), new NVec2(texture.bmWidth, texture.bmHeight));
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.DelayShort))
                {
                    ImGui.BeginTooltip();
                    ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
                    ImGui.TextUnformatted("Alpha Mask");
                    ImGui.PopTextWrapPos();
                    ImGui.EndTooltip();
                }
            }

            // Palette image 256=16x16 -> scaled up 128x128
            if (texPalHandle != -1)
            {
                ImGui.Image(new IntPtr(texPalHandle), new NVec2(128, 128));
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.DelayShort))
                {
                    ImGui.BeginTooltip();
                    ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
                    ImGui.TextUnformatted("Color Palette");
                    ImGui.PopTextWrapPos();
                    ImGui.EndTooltip();
                }
            }
        }

        ImGui.End();
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
        //ImGui.ShowDemoWindow();

        PresentGfxTextureWindow();

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
