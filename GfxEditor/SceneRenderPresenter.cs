using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using CommunityToolkit.HighPerformance;
using static GfxEditor.File3di;
using Engine.Graphics;
using static GfxEditor.TriangleDrawer;
using GfxEditor.Graphics;

namespace GfxEditor;

internal class SceneRenderPresenter : IDisposable
{
    // https://learnopengl.com/Advanced-OpenGL/Framebuffers
    // https://stackoverflow.com/questions/9261942/opentk-c-sharp-roatating-cube-example

    private readonly TriangleDrawer _triangleBatch;
    private readonly DebugDrawer _dbgDrawer;
    private readonly TextDrawer _textDrawer;
    private readonly GfxEdit _model;

    private int fbo;
    private int rbo;
    int texColor;
    //int texDepth;
    Vector2i fboSize = default;
    private readonly Window window;

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

    public SceneRenderPresenter(Window parent, GfxEdit model)
    {
        _triangleBatch = new TriangleDrawer();
        _model = model;
        _triangleBatch.OnLoad();
        window = parent;

        _dbgDrawer = new DebugDrawer(_triangleBatch._camera);
        _dbgDrawer.Initialize();

        _textDrawer = new TextDrawer(_triangleBatch._camera);
        _textDrawer.Initialize();

        model.FileUpdated += (sender, args) =>
        {
            RebuildTextures();
            _triangleBatch.FrameScene();
        };

        // https://github.com/ocornut/imgui/blob/master/docs/FAQ.md#q-how-can-i-tell-whether-to-dispatch-mousekeyboard-to-dear-imgui-or-my-application

        window.KeyDown += args =>
        {
            if(!window.IsFocused || !window.IsVisible || window.IsExiting)
                return;

            var io = ImGui.GetIO();
            if (!io.WantCaptureKeyboard)
                _triangleBatch.HandleKeyDown(args);
        };
        window.KeyUp += args =>
        {
            if (!window.IsFocused || !window.IsVisible || window.IsExiting)
                return;

            _triangleBatch.HandleKeyUp(args);
        };

        window.MouseDown += args =>
        {
            if (!window.IsFocused || !window.IsVisible || window.IsExiting)
                return;

            var io = ImGui.GetIO();
            if (!io.WantCaptureMouse)
            {
                _triangleBatch.HandleMouseDown(args);

                if(args.Button == MouseButton.Right)
                    _camEnabled = true;
            }
        };
        window.MouseUp += args =>
        {
            if (!window.IsFocused || !window.IsVisible || window.IsExiting)
                return;

            _triangleBatch.HandleMouseUp(args);

            if (args.Button == MouseButton.Right)
                _camEnabled = false;
        };
        window.MouseWheel += args =>
        {
            if (!window.IsFocused || !window.IsVisible || window.IsExiting)
                return;

            _triangleBatch.HandleMouseWheel(args);

            var ySign = Math.Sign(args.Offset.Y);
            if (ySign < 0) // Down
            {
                _triangleBatch.Arcball.UpdateZoom(3f, _triangleBatch.SceneSize);
            }
            else if (ySign > 0) // Up
            {
                _triangleBatch.Arcball.UpdateZoom(-3f, _triangleBatch.SceneSize);
            }

        };
        window.TextInput += args =>
        {
            if (!window.IsFocused || !window.IsVisible || window.IsExiting)
                return;

            var io = ImGui.GetIO();
            if (!io.WantTextInput)
                _triangleBatch.HandleText(args);
        };
    }

    private Vector2i _lastpos;
    public Vector2i GetCursorPosition()
    {
        var mouseState = window.MouseState;
        var screenSize = window.ClientSize;
        var newPoint = new Vector2i((int)mouseState.X, (int)mouseState.Y);
        var clientPos = newPoint;

        // prevents cursor being updated outside of window
        if (0 < clientPos.X && clientPos.X < screenSize.X &&
            0 < clientPos.Y && clientPos.Y < screenSize.Y)
        {
            _lastpos = clientPos;
        }

        return new Vector2i(_lastpos.X, _lastpos.Y);
    }

    private bool _camEnabled = false;
    private Vector2i _lastMousePosition;
    private void UpdateCameraDrag(TimeSpan dt)
    {
        // mouse orbit camera binding
        var newMousePos = GetCursorPosition();

        var delta = newMousePos - _lastMousePosition;
        _lastMousePosition = newMousePos;

        if (_camEnabled == false || delta.X == 0 && delta.Y == 0)
            return;

        _triangleBatch.Arcball.UpdateAngleInput(-delta.X, -delta.Y, (float)dt.TotalSeconds);
    }

    public void DrawViewportWindow(TimeSpan dt)
    {
        if(_triangleBatch is not null && _triangleBatch._renderTextures is not null)
            PushModelTriangles(_triangleBatch, _model);

        UpdateCameraDrag(dt);
        _dbgDrawer.Update(dt);

        _dbgDrawer._lineBatch.Append(Vector3.Zero, Vector3.UnitX * 0.25f, Color4.Red);
        _dbgDrawer._lineBatch.Append(Vector3.Zero, Vector3.UnitY * 0.25f, Color4.Green);
        _dbgDrawer._lineBatch.Append(Vector3.Zero, Vector3.UnitZ * 0.25f, Color4.Blue);

        // Draw text
        _textDrawer.Update(dt);

        GlError.Check();
        // DRAW WINDOW

        // https://gamedev.stackexchange.com/a/140704

        const string WindowViewClass = "Gfx View";
        ImGui.Begin(WindowViewClass);
        {
            // Using a Child allow to fill all the space of the window.
            // It also alows customization
            ImGui.BeginChild(WindowViewClass + "Child");

            // Get the size of the child (i.e. the whole draw size of the windows).
            System.Numerics.Vector2 wsize = ImGui.GetWindowSize();

            // make sure the buffers are the currect size
            Vector2i wsizei = new((int)wsize.X, (int)wsize.Y);
            if (fboSize != wsizei)
            {
                fboSize = wsizei;

                // create our frame buffer if needed
                if (fbo == 0)
                {
                    fbo = GL.GenFramebuffer();
                    // bind our frame buffer
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
                    GL.ObjectLabel(ObjectLabelIdentifier.Framebuffer, fbo, 10, WindowViewClass);
                }
                else
                {
                    // bind our frame buffer
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
                }

                if (texColor > 0)
                    GL.DeleteTexture(texColor);

                texColor = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, texColor);
                GL.ObjectLabel(ObjectLabelIdentifier.Texture, texColor, 16, "GameWindow:Color");
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, wsizei.X, wsizei.Y, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texColor, 0);

                if (rbo > 0)
                    GL.DeleteRenderbuffer(rbo);

                rbo = GL.GenRenderbuffer();
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo);
                GL.ObjectLabel(ObjectLabelIdentifier.Renderbuffer, rbo, 16, "GameWindow:Depth");
                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent32f, wsizei.X, wsizei.Y);
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, rbo);

                // make sure the frame buffer is complete
                FramebufferErrorCode errorCode = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
                if (errorCode != FramebufferErrorCode.FramebufferComplete)
                    throw new Exception();
            }
            else
            {
                // bind our frame and depth buffer
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo);
            }

            GlError.Check();
            GL.Viewport(0,0, wsizei.X, wsizei.Y); // change the viewport to window


            // Set clear color
            GL.ClearColor(new Color4(0, 64, 80, 255));

            // Clear the screen
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // actually draw the scene
            {
                //Draw triangle
                _triangleBatch.OnResize(wsizei.X, wsizei.Y);
                _dbgDrawer.Resize();
                _dbgDrawer.Render();
                _textDrawer.Resize();
                _textDrawer.Render();
                _triangleBatch.OnRenderFrame(dt);
                GlError.Check();
            }

            // unbind our bo so nothing else uses it
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            GL.Viewport(0, 0, window.ClientSize.X, window.ClientSize.Y); // back to full screen size

            GlError.Check();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texColor);
            // Because I use the texture from OpenGL, I need to invert the V from the UV.
            ImGui.Image(new IntPtr(texColor), wsize, System.Numerics.Vector2.UnitY, System.Numerics.Vector2.UnitX);

            // enables mouse input pass through
            // https://github.com/ocornut/imgui/issues/4831#issuecomment-1001522204
            var result = ImGui.IsItemHovered();
            if(result)
                ImGui.SetNextFrameWantCaptureMouse(false);

            GlError.Check();
            ImGui.EndChild();
        }
        ImGui.End();
    }

    public void Dispose()
    {
        _triangleBatch.OnUnload();
        _dbgDrawer.Destroy();
        _textDrawer.Destroy();
        GL.DeleteFramebuffer(fbo);
    }

    public void PresentUi()
    {
        _triangleBatch.PresentUi();
    }

    private void RebuildTextures()
    {
        // find max size of the textures, and square it
        var textures = _model.OpenedFile._textures;
        var maxTexSize = Vector2i.Zero;
        foreach (var texture in textures)
        {
            maxTexSize = Vector2i.ComponentMax(maxTexSize, new Vector2i(texture.bmWidth, texture.bmHeight));
        }

        // Allocate array textures
        if (_triangleBatch._renderTextures is not null)
        {
            _triangleBatch._renderTextures.Dispose();
            _triangleBatch._renderTextures = null;
        }
        var texArray = _triangleBatch._renderTextures = new GfxArrayTexture(maxTexSize.X, maxTexSize.Y, textures.Count);

        // Load tex data
        for (var i = 0; i < textures.Count; i++)
        {
            var texture = textures[i];
            var texels = new byte[texture.bmWidth * texture.bmHeight * sizeof(uint)];

            var numPixels = texture.bmWidth * texture.bmHeight;
            var stride = texture.bmSize / numPixels;

            var bmLines = _model.OpenedFile._bmLines[i].AsSpan();
            var palette = _model.OpenedFile._palettes[i].AsSpan().AsBytes();
            for (var iPx = 0; iPx < numPixels; iPx++)
            {
                // GL: [RGBA] 3DI Palette: [BGRA]
                var index = bmLines[iPx * stride + 0] * 4;
                texels[iPx * 4 + 3] = stride == 2 ? bmLines[iPx * stride + 1] : (byte)255; // A
                texels[iPx * 4 + 2] = palette[index + 0];
                texels[iPx * 4 + 1] = palette[index + 1];
                texels[iPx * 4 + 0] = palette[index + 2];
            }

            // add data to arrayTex
            texArray.UploadTexture(texture.bmWidth, texture.bmHeight, i, texels);
        }

        texArray.Finish();
    }

    private static void PushModelTriangles(TriangleDrawer triangleDrawer, GfxEdit gfxEdit)
    {
        // get all triangles from gfx active lod and push to TriangleBatch

        const CamoColor camo = CamoColor.Green;
        var gfx = gfxEdit.OpenedFile;

        if (gfx is null || gfx._header.nLODs == 0) return;

        var lod = gfxEdit.ActiveLod;

        for (var iBone = 0; iBone < gfx._lodSubObjects[lod].Length; iBone++)
        {
            var bone = gfx._lodSubObjects[lod][iBone];

            var foff = gfx.FaceOffset(lod, iBone); // offset into face array for bone
            var voff = gfx.VecOffset(lod, iBone); // offset into vertex array for bone

            for (var i = 0; i < bone.nFaces; i++)
            {
                var face = gfx._lodFaces[lod][i + foff];
                var material = gfx._lodMaterials[lod][face.MaterialIndex];
                var texIndex = material.TexIndex(camo);
                var texture = gfx._textures[texIndex];
                var norms = gfx._lodNormals[lod];
                var isTransparent = texture.bmSize / (texture.bmWidth * texture.bmHeight) == 2;

                var boneOffset = new Vector4() { X = bone.VecXoff >> 8, Y = bone.VecYoff >> 8, Z = bone.VecZoff >> 8 };

                {
                    //TODO: Make the face use arrays
                    var vertPos = gfx._lodPositions[lod][face.Vertex1 + voff];
                    var tkVPos = new Vector4(vertPos.x, vertPos.y, vertPos.z, vertPos.w);
                    var pos = (tkVPos - boneOffset).Xyz / 256;
                    var norm = norms[face.Normal1];
                    var nor = new Vector3(norm.x, norm.y, norm.z);
                    var coords = new Vector2(face.tu1 / 65536.0f, face.tv1 / 65536.0f);
                    var color = Color4.White;
                    color.A = isTransparent ? 0 : 1;
                    var vert = new TriangleDrawer.VertexTex(pos, color, nor, new Vector3(coords.X, coords.Y, texIndex));
                    triangleDrawer.Append(in vert);

                    vertPos = gfx._lodPositions[lod][face.Vertex2 + voff];
                    tkVPos = new Vector4(vertPos.x, vertPos.y, vertPos.z, vertPos.w);
                    pos = (tkVPos - boneOffset).Xyz / 256;
                    norm = norms[face.Normal2];
                    nor = new Vector3(norm.x, norm.y, norm.z);
                    coords = new Vector2(face.tu2 / 65536.0f, face.tv2 / 65536.0f);
                    //color = Color4.White;
                    //color.A = isTransparent ? 0 : 1;
                    vert = new TriangleDrawer.VertexTex(pos, color, nor, new Vector3(coords.X, coords.Y, texIndex));
                    triangleDrawer.Append(in vert);

                    vertPos = gfx._lodPositions[lod][face.Vertex3 + voff];
                    tkVPos = new Vector4(vertPos.x, vertPos.y, vertPos.z, vertPos.w);
                    pos = (tkVPos - boneOffset).Xyz / 256;
                    norm = norms[face.Normal3];
                    nor = new Vector3(norm.x, norm.y, norm.z);
                    coords = new Vector2(face.tu3 / 65536.0f, face.tv3 / 65536.0f);
                    //color = Color4.White;
                    //color.A = isTransparent ? 0 : 1;
                    vert = new TriangleDrawer.VertexTex(pos, color, nor, new Vector3(coords.X, coords.Y, texIndex));
                    triangleDrawer.Append(in vert);
                }
            }
        }
    }
}
