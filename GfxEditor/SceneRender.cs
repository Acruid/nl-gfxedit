using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace GfxEditor;

internal class SceneRender : IDisposable
{
    // https://learnopengl.com/Advanced-OpenGL/Framebuffers
    // https://stackoverflow.com/questions/9261942/opentk-c-sharp-roatating-cube-example

    private IModelDrawer _drawer;

    int fbo;
    int rbo;
    int texColor;
    //int texDepth;
    Vector2i fboSize = default;
    private readonly Window window;

    public SceneRender(Window window, IModelDrawer drawer)
    {
        _drawer = drawer;
        _drawer.OnLoad();
        this.window = window;

        // https://github.com/ocornut/imgui/blob/master/docs/FAQ.md#q-how-can-i-tell-whether-to-dispatch-mousekeyboard-to-dear-imgui-or-my-application

        window.KeyDown += args =>
        {
            if(!window.IsFocused || !window.IsVisible || window.IsExiting)
                return;

            var io = ImGui.GetIO();
            if (!io.WantCaptureKeyboard)
                _drawer.HandleKeyDown(args);
        };
        window.KeyUp += args =>
        {
            if (!window.IsFocused || !window.IsVisible || window.IsExiting)
                return;

            _drawer.HandleKeyUp(args);
        };

        window.MouseDown += args =>
        {
            if (!window.IsFocused || !window.IsVisible || window.IsExiting)
                return;

            var io = ImGui.GetIO();
            if (!io.WantCaptureMouse)
                _drawer.HandleMouseDown(args);
        };
        window.MouseUp += args =>
        {
            if (!window.IsFocused || !window.IsVisible || window.IsExiting)
                return;

            _drawer.HandleMouseUp(args);
        };
        window.MouseWheel += args =>
        {
            if (!window.IsFocused || !window.IsVisible || window.IsExiting)
                return;

            _drawer.HandleMouseWheel(args);
        };
        window.TextInput += args =>
        {
            if (!window.IsFocused || !window.IsVisible || window.IsExiting)
                return;

            var io = ImGui.GetIO();
            if (!io.WantTextInput)
                _drawer.HandleText(args);
        };
    }

    public void DrawViewportWindow()
    {
        GlError.Check();
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

                // bind our frame buffer
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

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
                //GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

                //texDepth = GL.GenTexture();
                //GL.BindTexture(TextureTarget.Texture2D, texDepth);
                //GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32f, 800, 600, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
                //GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, texDepth, 0);

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

            // actually draw the scene
            {
                //Draw triangle
                _drawer.OnResize(wsizei.X, wsizei.Y);
                _drawer.OnRenderFrame();
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
        _drawer.OnUnload();
        GL.DeleteFramebuffer(fbo);
    }

    public void PresentUi()
    {
        _drawer.PresentUi();
    }
}
