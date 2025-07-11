﻿using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using Engine.ImGuiBindings;
using GfxEditor.Graphics;
using GfxEditor.ImGuiTools;
using ImGuiNET;
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
public class Window : GameWindow
{
    private readonly GfxEdit _gfxEdit;
    private ImGuiController _controller;
    private SceneRenderPresenter _scene;
    private LodDataPresenter _lodWindow;

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

        _scene = new SceneRenderPresenter(this, _gfxEdit);
        _lodWindow = new LodDataPresenter(this, _gfxEdit);
        _gfxEdit.FileUpdated += (sender, args) =>
        {
            textureDirty = true;
            _gfxEdit.ActiveLod = GfxModelWindow_SelectedLodIdx = 0;
        };

        GlError.Check();
    }

    private const string GuiModelWindowClass = "Gfx Model";
    private int GfxModelWindow_SelectedLodIdx = 0;
    private void PresentGfxModelWindow()
    {
        if (!(_gfxEdit is not null && _gfxEdit.OpenedFile is not null))
            return;

        ref var header = ref _gfxEdit.OpenedFile._header;

        ImGui.Begin(GuiModelWindowClass);

        header.Name = ImGuiEx.InputString("Model Name", header.Name) ?? string.Empty;

        if (ImGui.BeginCombo("Render Func", Enum.GetName(header.RenderType)))
        {
            var values = Enum.GetValues<LodRenderType_V8>();

            for (var n = 0; n < values.Length; n++)
            {
                var is_selected = values[n] == header.RenderType;
                if (ImGui.Selectable(Enum.GetName(values[n]), is_selected))
                    if (values[n] != header.RenderType)
                    {
                        header.RenderType = values[n];
                        // rendertype event
                    }

                if (is_selected)
                    ImGui.SetItemDefaultFocus();
            }

            ImGui.EndCombo();
        }

        ImGui.Text("LODs:");

        var lods = _gfxEdit.OpenedFile._lodHeaders;

        // prevents selected from going out of bounds when lods count changes.
        if (GfxModelWindow_SelectedLodIdx > lods.Count)
        {
            GfxModelWindow_SelectedLodIdx = 0;
        }

        if (ImGui.BeginListBox("##UNIQUE_LBL_1", new NVec2(-1, 5 * ImGui.GetTextLineHeightWithSpacing())))
        {
            for (int iLod = 0; iLod < lods.Count; iLod++)
            {
                bool is_selected = (GfxModelWindow_SelectedLodIdx == iLod);
                if (ImGui.Selectable($"{LodTranslate(iLod)}", is_selected))
                {
                    _gfxEdit.ActiveLod = GfxModelWindow_SelectedLodIdx = iLod;
                }

                // Set the initial focus when opening the combo (scrolling + keyboard navigation focus)
                if (is_selected)
                    ImGui.SetItemDefaultFocus();
            }

            ImGui.EndListBox();
        }

        var lodIdx = GfxModelWindow_SelectedLodIdx;

        var val = header.get_MinLodDist(lodIdx);
        if (ImGui.InputInt("Min Dist", ref val))
        {
            header.set_MinLodDist(lodIdx, val);
        }

        ImGui.End();
    }

    private string LodTranslate(int lodNum)
    {
        switch (lodNum)
        {
            case 0:
                return "HIGH";
            case 1:
                return "MED";
            case 2:
                return "LOW";
            case 3:
                return "TINY";
        }

        return string.Empty;
    }

    private const string GuiTextureWindowClass = "Gfx Textures";

    bool GfxTextureWindow_ShowOpenTexModal = false;

    int GfxTextureWindow_SelectedTexIdx = 0;
    int GfxTextureWindow_TexColorHandle = -1;
    int GfxTextureWindow_TexAlphaHandle = -1;
    int GfxTextureWindow_TexPalHandle = -1;
    private bool textureDirty = false;
    private void PresentGfxTextureWindow()
    {
        if (!(_gfxEdit is not null && _gfxEdit.OpenedFile is not null && _gfxEdit.OpenedFile._textures is not null))
            return;

        List<TEXTURE> _textures = _gfxEdit.OpenedFile._textures;
        var texDirty = textureDirty;

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

        if (ImGui.Button("Import"))
        {
            GfxTextureWindow_ShowOpenTexModal = true;
        }

        ImGui.SameLine();

        if (_textures.Count == 0) ImGui.BeginDisabled();

        if (ImGui.Button("Export"))
        {
            var n = GfxTextureWindow_SelectedTexIdx;
            var texture = _textures[n];
            var name = ImGuiComDlg.SanitizeFileName($"{texture.Name}.TIF");
            _gfxEdit.ExportImage(GfxTextureWindow_SelectedTexIdx, new FileInfo(name));
        }

        ImGui.SameLine();

        if (ImGui.Button("Export All"))
        {
            foreach (var texture in _textures)
            {
                var name = ImGuiComDlg.SanitizeFileName($"{texture.Name}.TIF");
                _gfxEdit.ExportImage(GfxTextureWindow_SelectedTexIdx, new FileInfo(name));
            }
        }

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
                textureDirty = false;
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

                if(stride == 2)
                {
                    GfxTextureWindow_TexAlphaHandle = texAlphaHandle = GenTexture();
                    BindTexture(TextureTarget.Texture2D, texAlphaHandle);
                    ObjectLabel(ObjectLabelIdentifier.Texture, texAlphaHandle, 12, "GfxWnd:Alpha");
                    TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, texture.bmWidth, texture.bmHeight, 0, PixelFormat.Rgb, PixelType.UnsignedByte, texAlphaPixels);
                    TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                    TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                }

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


    private const string GuiAnmWindowClass = "Animations";
    private void PresentAnmWindow()
    {
        ref var header = ref _gfxEdit.OpenedFile._header;

        var headers = _gfxEdit.OpenedFile._lodHeaders;
        if (headers.Count <= 0 || (headers[_gfxEdit.ActiveLod].Flags & LodHeaderFlags.OffsetArmatures) == 0)
            return;

        ImGui.Begin(GuiAnmWindowClass);

        var disabled = true;

        ImGui.Text("Load KSA...");
        ImGui.SameLine();

        var text = _gfxEdit.LoadedKsaPath ?? string.Empty;
        if(ImGui.InputText("##KsaFileName", ref text, 260, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            if(string.IsNullOrEmpty(text))
            {
                _gfxEdit.UnloadKsa();
            }
            if (File.Exists(text))
            {
                _gfxEdit.LoadKsa(text);
            }
        }

        ImGui.Text("Load ANM...");
        ImGui.SameLine();

        text = _gfxEdit.LoadedAnmPath ?? string.Empty;
        if (ImGui.InputText("##AnmFileName", ref text, 260, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            if (string.IsNullOrEmpty(text))
            {
                _gfxEdit.UnloadAnm();
            }
            if (File.Exists(text))
            {
                _gfxEdit.LoadAnm(text);
            }
        }

        // anm dropdown

        var anmValue = _gfxEdit.CurrentAnimation;
        var anmDefs = _gfxEdit.LoadedAnm?.GetDefs();
        string defName = string.Empty;
        if (anmDefs is not null)
        {
            var def = anmDefs.FirstOrDefault(def => def.AnimationNumber == anmValue);
            if (def.Move is not null)
            {
                defName = def.Move;
            }
        }
        string name = $"{anmValue} {defName}";

        if (ImGui.BeginCombo("Animation", name))
        {
            if (_gfxEdit.LoadedKsa is not null)
            {
                var values = _gfxEdit.LoadedKsa.GetAnimations();

                for (var n = 0; n < values.Length; n++)
                {
                    defName = string.Empty;
                    if (anmDefs is not null)
                    {
                        var def = anmDefs.FirstOrDefault(def => def.AnimationNumber == n);
                        if (def.Move is not null)
                        {
                            defName = def.Move;
                        }
                    }

                    name = $"{n} {defName}";
                    var selected = n == _gfxEdit.CurrentAnimation;
                    if (ImGui.Selectable(name, selected))
                    {
                        _gfxEdit.CurrentAnimation = n;
                    }

                    if (selected)
                        ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }

        // keyframe timeline slider
        int frameCount;
        if (_gfxEdit.LoadedKsa is null)
            frameCount = 0;
        else
        {
            frameCount = _gfxEdit.LoadedKsa.GetAnimations()[_gfxEdit.CurrentAnimation].numKeyframes;
        }

        var curFrame = _gfxEdit.CurrentKeyframe;
        if(ImGui.SliderInt("TimeLine", ref curFrame, 0, frameCount - 1))
        {
            _gfxEdit.CurrentKeyframe = curFrame;
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
    private bool _showSaveAsGfxModal = false;

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
                _gfxEdit.NewFile();
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
            if (ImGui.MenuItem("Save As..."))
            {
                _showSaveAsGfxModal = true;
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
        PresentGfxModelWindow();
        PresentAnmWindow();
        _lodWindow.Present();
        _scene.PresentUi();

        GlError.Check();
        _scene.DrawViewportWindow(TimeSpan.FromSeconds(e.Time));

        GlError.Check();
        _controller.EndDockspace();

        {
            if (ImGuiComDlg.ShowModalDialog("modal-comItemDlg", ref _showGameDirModal, ComDlgType.FolderSelectDialog))
            {
                if (ImGuiComDlg.GetLastResult() == ComDlgResult.Ok)
                {
                    var fullPath = ImGuiComDlg.GetLastPath();
                    _gfxEdit.OpenDirectory(new DirectoryInfo(fullPath));
                }
            }
        }

        {
            //TODO: Non-const path
            const string startingPath = @"R:\Novalogic\Documents\Archive\3DI's\TESTING 3DI's\";
            if (ImGuiComDlg.ShowModalDialog("modal-comItemDlg", ref _showOpenGfxModal, ComDlgType.FileOpenDialog, ".3di", new DirectoryInfo(startingPath)))
            {
                if (ImGuiComDlg.GetLastResult() == ComDlgResult.Ok)
                {
                    var fullPath = ImGuiComDlg.GetLastPath();
                    _gfxEdit.LoadFile(new FileInfo(fullPath));
                }
            }
        }

        {
            //TODO: Non-const path
            const string startingPath = @"R:\Novalogic\Documents\Archive\3DI's\TESTING 3DI's\";
            if (ImGuiComDlg.ShowModalDialog("modal-comItemDlg", ref _showSaveAsGfxModal, ComDlgType.FileSaveDialog, ".3di", new DirectoryInfo(startingPath)))
            {
                if (ImGuiComDlg.GetLastResult() == ComDlgResult.Ok)
                {
                    var fullPath = ImGuiComDlg.GetLastPath();
                    _gfxEdit.SaveFile(new FileInfo(fullPath));
                }
            }
        }

        {
            var startingPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (ImGuiComDlg.ShowModalDialog("modal-comItemDlg", ref GfxTextureWindow_ShowOpenTexModal, ComDlgType.FileOpenDialog, ".tif", new DirectoryInfo(startingPath)))
            {
                if (ImGuiComDlg.GetLastResult() == ComDlgResult.Ok)
                {
                    var fullPath = ImGuiComDlg.GetLastPath();

                    //TODO: Better insert logic
                    if (GfxTextureWindow_SelectedTexIdx == 0 && _gfxEdit.OpenedFile._textures.Count == 0)
                        GfxTextureWindow_SelectedTexIdx = _gfxEdit.AddTexture();

                    _gfxEdit.ReplaceTexture(GfxTextureWindow_SelectedTexIdx, new FileInfo(fullPath));
                }
            }
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

    protected override void OnFileDrop(FileDropEventArgs e)
    {
        base.OnFileDrop(e);

        var files = e.FileNames;

        foreach (var file in files)
        {
            string ext = Path.GetExtension(file).ToUpperInvariant();
            switch (ext)
            {
                case ".3DI":
                    _gfxEdit.LoadFile(new FileInfo(file));
                    break;

                case ".KSA":
                    _gfxEdit.LoadKsa(file);
                    break;
                    
                case ".ANM":
                    _gfxEdit.LoadAnm(file);
                    break;

            }
        }

    }

    protected override void OnUnload()
    {
        _scene.Dispose();
        _controller.Dispose();

        base.OnUnload();
    }
}
