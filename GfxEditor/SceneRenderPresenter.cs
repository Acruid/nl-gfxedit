using System.Runtime.InteropServices;
using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using CommunityToolkit.HighPerformance;
using static GfxEditor.File3di;
using Engine.Graphics;
using GfxEditor.Graphics;
using MathNet.Numerics.LinearAlgebra;
using Vector3 = OpenTK.Mathematics.Vector3;
using Color4 = OpenTK.Mathematics.Color4;

namespace GfxEditor;

public class SceneRenderPresenter : IDisposable
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
    Vector2i fboSize;
    private readonly Window window;

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

        model.FileUpdated += (_, _) => { RebuildScene(); };

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

        RebuildScene();
    }

    private void RebuildScene()
    {
        if(_model.OpenedFile is null)
            return;

        RebuildTextures();
        _triangleBatch.FrameScene();
    }

    public bool DrawModelSkins { get; set; } = true;
    public bool DrawModelCollision { get; set; }
    public bool DrawModelSkeleton { get; set; }
    public bool AnimateModel { get; set; }

    private Vector2i _lastClientPointerPosition;
    private Vector2i GetCursorPosition()
    {
        var mouseState = window.MouseState;
        var screenSize = window.ClientSize;
        var clientPos = new Vector2i((int)mouseState.X, (int)mouseState.Y);

        // prevents cursor being updated outside of window
        if (0 < clientPos.X && clientPos.X < screenSize.X &&
            0 < clientPos.Y && clientPos.Y < screenSize.Y)
        {
            _lastClientPointerPosition = clientPos;
        }

        return new Vector2i(_lastClientPointerPosition.X, _lastClientPointerPosition.Y);
    }

    private bool _camEnabled;
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
            PushModelTriangles(_triangleBatch, _model, _dbgDrawer);

        _anmTimeAccumulator += dt;

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
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, wsizei.X, wsizei.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
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

    private void RenderCameraUi(TriangleDrawer triangleDrawer, ArcballCameraController arcBallCam)
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
            TriangleDrawer.ResetCamera(arcBallCam);

        if (ImGui.Button("Fit Scene"))
            triangleDrawer.FrameNextScene = true;

        ImGui.Separator();

        var toggle = DrawModelSkins;
        ImGui.Checkbox("Draw Model", ref toggle);
        DrawModelSkins = toggle;

        toggle = DrawModelCollision;
        ImGui.Checkbox("Draw Collision", ref toggle);
        DrawModelCollision = toggle;

        toggle = DrawModelSkeleton;
        ImGui.Checkbox("Draw Skeleton", ref toggle);
        DrawModelSkeleton = toggle;

        toggle = AnimateModel;
        ImGui.Checkbox("Animate Model", ref toggle);
        AnimateModel = toggle;
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
        RenderCameraUi(_triangleBatch, _triangleBatch.Arcball);
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

        if (textures.Count == 0)
            maxTexSize = new Vector2i(64, 64);

        // Allocate array textures
        if (_triangleBatch._renderTextures is not null)
        {
            _triangleBatch._renderTextures.Dispose();
            _triangleBatch._renderTextures = null;
        }
        var texArray = _triangleBatch._renderTextures = new GfxArrayTexture(maxTexSize.X, maxTexSize.Y, textures.Count + 1);

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
            texArray.UploadTexture(texture.bmWidth, texture.bmHeight, i + 1, texels);
        }

        texArray.Finish();
    }

    private void PushModelTriangles(TriangleDrawer triangleDrawer, GfxEdit gfxEdit, DebugDrawer dbg)
    {
        // get all triangles from gfx active lod and push to TriangleBatch
        var gfx = gfxEdit.OpenedFile;

        if (gfx is null || gfx._header.nLODs == 0) return;

        var lod = gfxEdit.ActiveLod;
        var camo = gfxEdit.Camouflage;

        var bones = gfx._lodSubObjects[lod];

        var boneTransforms = new Matrix4[bones.Length];
        boneTransforms[0] = Matrix4.Identity;

        if (AnimateModel && TryGetKeyframe(gfxEdit, out var keyFrame))
        {
            float anmHeight = new FixedQ7_8(keyFrame.Height);
            ReadOnlySpan<byte> angles = MemoryMarshal.CreateSpan(ref keyFrame, 1).AsBytes().Slice(0, 15 * 4);
            Skeleton.CalculateBoneTransforms(bones, angles, anmHeight, boneTransforms);
        }
        else
        {
            for (var i = 0; i < boneTransforms.Length; i++)
            {
                boneTransforms[i] = Matrix4.Identity;
            }
        }

        if(DrawModelSkins)
            DrawSkinnedMeshes(triangleDrawer, gfx, lod, camo, boneTransforms);

        if(DrawModelCollision)
            DrawCollisionVolumes(triangleDrawer, dbg, gfx, lod);

        if (DrawModelSkeleton) 
            DrawSkeleton(dbg._lineBatch, bones, boneTransforms);
    }

    private TimeSpan _anmTimeAccumulator = TimeSpan.Zero;
    private unsafe void DrawSkinnedMeshes(TriangleDrawer triangleDrawer, File3di gfx, int lod, CamoColor camo,
        Matrix4[] boneTransforms)
    {
        var header = gfx._lodHeaders[lod];

        // Calculate the binding pose for each bone (move the mesh verts local to the bone)
        Matrix4[] bindingPose = new Matrix4[gfx._lodSubObjects[lod].Length];

        for (var iBone = 0; iBone < gfx._lodSubObjects[lod].Length; iBone++)
        {
            var bone = gfx._lodSubObjects[lod][iBone];

            bindingPose[iBone] = Matrix4.CreateTranslation(-bone.ModelPosition);
            Matrix4 boneMatrix = bindingPose[iBone] * boneTransforms[iBone] * Matrix4.Invert(bindingPose[iBone]);

            var foff = gfx.FaceOffset(lod, iBone); // offset into face array for bone
            var voff = gfx.VecOffset(lod, iBone); // offset into vertex array for bone

            for (var i = 0; i < bone.nFaces; i++)
            {
                var face = gfx._lodFaces[lod][i + foff];
                var material = gfx._lodMaterials[lod][face.MaterialIndex];
                var texIndex = material.TexIndex(camo);
                
                bool animated = (material.Transparentflag & 0b0100_0000) != 0;
                if(animated)
                {
                    var nFrames = header.texLoopCount;
                    var frameDelay = header.loopInterval;

                    const int gfxFps = 60;
                    var frames = _anmTimeAccumulator.TotalSeconds * gfxFps / frameDelay;
                    texIndex = (byte)(frames % nFrames);
                }

                var texture = gfx._textures[texIndex];
                var norms = gfx._lodNormals[lod];
                var isTransparent = texture.bmSize / (texture.bmWidth * texture.bmHeight) == 2;
                
                for (var iVertex = 0; iVertex < 3; iVertex++)
                {
                    Vector4 gfxPos = gfx._lodPositions[lod][face.PositonIdx[iVertex] + voff];
                    Vector3 modelPos = Vector3.TransformPosition(gfxPos.Xyz, boneMatrix);

                    var normal = ((Vector4)norms[face.NormalIdx[iVertex]]).Xyz;

                    var texCoords = new Vector2(face.texCoordU[iVertex] / 65536.0f, face.texCoordV[iVertex] / 65536.0f);

                    var color = Color4.White;
                    color.A = isTransparent ? 0.99f : 1; //TODO: There needs to be a better way to signal this, check the tex?

                    var vertex = new TriangleDrawer.VertexTex(modelPos, color, normal,
                        new Vector3(texCoords.X, texCoords.Y, texIndex + 1));
                    triangleDrawer.Append(in vertex);
                }
            }
        }
    }

    private static void DrawCollisionVolumes(TriangleDrawer triangleDrawer, DebugDrawer dbg, File3di gfx, int lod)
    {
        int planeIdx = 0;
        for (var iColVol = 0; iColVol < gfx._lodColVolumes[lod].Length; iColVol++)
        {
            var colVol = gfx._lodColVolumes[lod][iColVol];

            // seems like the AABB center
            {
                var volCenter = new Vector3(colVol.XMedian, colVol.YMedian, colVol.ZMedian);

                var center = volCenter;
                var halfx = Vector3.UnitX * 0.15f;
                var halfy = Vector3.UnitY * 0.15f;
                var halfz = Vector3.UnitZ * 0.15f;

                dbg._lineBatch.Append(-halfx + center, halfx + center, Color4.LightSkyBlue);
                dbg._lineBatch.Append(-halfy + center, halfy + center, Color4.LightSkyBlue);
                dbg._lineBatch.Append(-halfz + center, halfz + center, Color4.LightSkyBlue);
            }

            var aabbMin = new Vector3(colVol.xMin, colVol.yMin, colVol.zMin);
            var aabbMax = new Vector3(colVol.xMax, colVol.yMax, colVol.zMax);
            {
                DrawBox(dbg._lineBatch, aabbMin, aabbMax, Color4.LightSkyBlue);
            }

            var planes = new List<(Vector3 normal, float distance)>(colVol.CollisionPlaneCount);
            for (var iColPlane = 0; iColPlane < colVol.CollisionPlaneCount; iColPlane++)
            {
                var plane = gfx._lodColPlanes[lod][planeIdx];
                planeIdx++;

                var normal = new Vector3(plane.x, plane.y, plane.z);
                var distance = -plane.distance;
                planes.Add((normal, distance));
            }

            var volVerts = PlanesToVertices(planes);
            var triVerts = ConvexHull3D.CreateConvexHull(volVerts);

            var color = Color4.LightSkyBlue;
            color.A = 0.15f;

            foreach (var vert in triVerts)
            {
                var vertex = new TriangleDrawer.VertexTex(vert.position, color, vert.normal, new Vector3(0, 0, 0));
                triangleDrawer.Append(in vertex);
            }

            if(triVerts.Count == 0) continue;
            var lineBatch = dbg._lineBatch;
            color = Color4.DeepSkyBlue;
            for (int i = 0; i < triVerts.Count; i += 3)
            {
                // Draw the first edge
                lineBatch.Append(triVerts[i].position, triVerts[i + 1].position, color);
                // Draw the second edge
                lineBatch.Append(triVerts[i + 1].position, triVerts[i + 2].position, color);
                // Draw the third edge
                lineBatch.Append(triVerts[i + 2].position, triVerts[i].position, color);
            }
        }
    }

    private static void DrawSkeleton(LineBatch lines, ModelSegmentMesh[] bones, Matrix4[] boneTransforms)
    {
        // Calculate the binding pose for each bone (move the mesh verts local to the bone)
        Matrix4[] bindingPose = new Matrix4[bones.Length];
        for (var i = 0; i < bones.Length; i++)
        {
            var bone = bones[i];
            bindingPose[i] = Matrix4.CreateTranslation(-bone.ModelPosition);
        }

        for (var iBone = 0; iBone < bones.Length; iBone++)
        {
            var bone = bones[iBone];
            var pBone = bones[bone.parentBone];

            int boneIndex = iBone;
            int pBoneIndex = bone.parentBone;

            Matrix4 boneMatrix = bindingPose[boneIndex] * boneTransforms[boneIndex] * Matrix4.Invert(bindingPose[boneIndex]);
            Matrix4 pboneMatrix = bindingPose[pBoneIndex] * boneTransforms[pBoneIndex] * Matrix4.Invert(bindingPose[pBoneIndex]);

            Vector3 bonePosition = Vector3.TransformPosition(bone.ModelPosition, boneMatrix);
            Vector3 rotIndicator = Vector3.TransformPosition(bone.ModelPosition + Vector3.UnitX * 0.1f, boneMatrix);
            Vector3 pBonePosition = Vector3.TransformPosition(pBone.ModelPosition, pboneMatrix);
            
            lines.Append(bonePosition, rotIndicator, Color4.LimeGreen);
            lines.Append(pBonePosition, bonePosition, Color4.Green);
            lines.Append(bonePosition, 0.05f, Color4.LightGreen);
        }
    }

    private static bool TryGetKeyframe(GfxEdit gfxEdit, out FileKsa.KEYFRAME keyframe)
    {
        if (gfxEdit.LoadedKsa is null)
        {
            keyframe = default;
            return false;
        }

        var ksaAnm = gfxEdit.LoadedKsa.GetAnimations();
        var curAnmIdx = gfxEdit.CurrentAnimation;
        var curAnm = ksaAnm[gfxEdit.CurrentAnimation];
        var keyFrames = gfxEdit.LoadedKsa.GetKeyframes();

        var keyframeIdx = 0;
        for (var i = 0; i < curAnmIdx; i++)
        {
            keyframeIdx += ksaAnm[i].numKeyframes;
        }

        keyframeIdx += gfxEdit.CurrentKeyframe;
        keyframe = keyFrames[keyframeIdx];
        return true;
    }

    private static void DrawBox(LineBatch lines, Vector3 min, Vector3 max, Color4 color)
    {
        Vector3 v1 = new Vector3(min.X, min.Y, min.Z);
        Vector3 v2 = new Vector3(max.X, min.Y, min.Z);
        Vector3 v3 = new Vector3(max.X, max.Y, min.Z);
        Vector3 v4 = new Vector3(min.X, max.Y, min.Z);
        Vector3 v5 = new Vector3(min.X, min.Y, max.Z);
        Vector3 v6 = new Vector3(max.X, min.Y, max.Z);
        Vector3 v7 = new Vector3(max.X, max.Y, max.Z);
        Vector3 v8 = new Vector3(min.X, max.Y, max.Z);

        lines.Append(v1, v2, color);
        lines.Append(v2, v3, color);
        lines.Append(v3, v4, color);
        lines.Append(v4, v1, color);

        lines.Append(v5, v6, color);
        lines.Append(v6, v7, color);
        lines.Append(v7, v8, color);
        lines.Append(v8, v5, color);

        lines.Append(v1, v5, color);
        lines.Append(v2, v6, color);
        lines.Append(v3, v7, color);
        lines.Append(v4, v8, color);
    }

    public static List<Vector3> PlanesToVertices(List<(Vector3 normal, float distance)> planes)
    {
        // Create a list to store the vertices of the mesh
        List<Vector3> vertices = new List<Vector3>();

        // Iterate over each triplet of planes
        for (int i = 0; i < planes.Count; i++)
        {
            for (int j = i + 1; j < planes.Count; j++)
            {
                for (int k = j + 1; k < planes.Count; k++)
                {
                    // Create a matrix to solve for the point of intersection
                    Matrix<float> A = Matrix<float>.Build.DenseOfRowArrays(
                        new float[] { planes[i].normal.X, planes[i].normal.Y, planes[i].normal.Z },
                        new float[] { planes[j].normal.X, planes[j].normal.Y, planes[j].normal.Z },
                        new float[] { planes[k].normal.X, planes[k].normal.Y, planes[k].normal.Z }
                    );
                    Vector<float> b = Vector<float>.Build.Dense(new float[] { planes[i].distance, planes[j].distance, planes[k].distance });

                    // Check if the matrix is invertible
                    if (A.Determinant() != 0)
                    {
                        // Solve for the point of intersection
                        Vector<float> x = A.Solve(b);

                        // Check if the point of intersection is inside the convex hull
                        Vector3 point = new Vector3(x[0], x[1], x[2]);
                        bool inside = true;
                        foreach (var plane in planes)
                        {
                            if (Vector3.Dot(plane.normal, point) > plane.distance + 1e-6)
                            {
                                inside = false;
                                break;
                            }
                        }

                        // Add the point of intersection to the list of vertices if it is inside the convex hull
                        if (inside)
                            vertices.Add(point);
                    }
                }
            }
        }

        return vertices;
    }
}
