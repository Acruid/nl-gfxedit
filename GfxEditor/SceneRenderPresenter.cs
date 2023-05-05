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
            PushModelTriangles(_triangleBatch, _model, _dbgDrawer);

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

    private static void DrawPoint(DebugDrawer drawer, Vector4 pos)
    {
        drawer._lineBatch.Append(Vector3.UnitX * -0.15f + pos.Xyz, Vector3.UnitX * 0.15f + pos.Xyz, Color4.Pink);
        drawer._lineBatch.Append(Vector3.UnitY * -0.15f + pos.Xyz, Vector3.UnitY * 0.15f + pos.Xyz, Color4.Pink);
        drawer._lineBatch.Append(Vector3.UnitZ * -0.15f + pos.Xyz, Vector3.UnitZ * 0.15f + pos.Xyz, Color4.Pink);
    }

    private static unsafe void PushModelTriangles(TriangleDrawer triangleDrawer, GfxEdit gfxEdit, DebugDrawer dbg)
    {
        // get all triangles from gfx active lod and push to TriangleBatch
        var gfx = gfxEdit.OpenedFile;

        if (gfx is null || gfx._header.nLODs == 0) return;

        var lod = gfxEdit.ActiveLod;
        var camo = gfxEdit.Camouflage;

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

                var boneOffset = new Vector4 { X = (byte) bone.VecXoff >> 8, Y = (byte) bone.VecYoff >> 8, Z = (byte) bone.VecZoff >> 8, W = 0 };

                for(var iVertex = 0; iVertex < 3; iVertex++)
                {
                    Vector4 segPos = gfx._lodPositions[lod][face.PositonIdx[iVertex] + voff];
                    var modelPos = (segPos - boneOffset).Xyz / 256;

                    var normal = ((Vector4)norms[face.NormalIdx[iVertex]]).Xyz;

                    var texCoords = new Vector2(face.texCoordU[iVertex] / 65536.0f, face.texCoordV[iVertex] / 65536.0f);

                    var color = Color4.White;
                    color.A = isTransparent ? 0 : 1;

                    var vertex = new TriangleDrawer.VertexTex(modelPos, color, normal, new Vector3(texCoords.X, texCoords.Y, texIndex));
                    triangleDrawer.Append(in vertex);
                }
            }
        }

        int planeIdx = 0;
        for(var iColVol = 0; iColVol < gfx._lodColVolumes[lod].Length; iColVol++)
        {
            var colVol = gfx._lodColVolumes[lod][iColVol];

            // seems like the AABB center
            {
                var volCenter = new Vector3(colVol.XMedian, colVol.YMedian, colVol.ZMedian);

                var center = volCenter;
                var halfx = Vector3.UnitX * 0.15f;
                var halfy = Vector3.UnitY * 0.15f;
                var halfz = Vector3.UnitZ * 0.15f;

                dbg._lineBatch.Append(-halfx + center, halfx + center, Color4.Coral);
                dbg._lineBatch.Append(-halfy + center, halfy + center, Color4.Coral);
                dbg._lineBatch.Append(-halfz + center, halfz + center, Color4.Coral);
            }

            var aabbMin = new Vector3(colVol.xMin, colVol.yMin, colVol.zMin);
            var aabbMax = new Vector3(colVol.xMax, colVol.yMax, colVol.zMax);
            {
                //DrawBox(dbg._lineBatch, aabbMin, aabbMax, Color4.White);
            }

            for (var iColPlane = 0; iColPlane < colVol.nColPlanes; iColPlane++)
            {
                var plane = gfx._lodColPlanes[lod][planeIdx];
                planeIdx++;

                var normal = new Vector3(plane.x / 16384.0f, plane.y / 16384.0f, plane.z / 16384.0f);
                var distance = -plane.distance / 256f;
                
                 var color = new Color4(normal.X, normal.Y, normal.Z, 1);
                var verts = ClipPlaneToPolygon(aabbMin, aabbMax, normal, distance);
                DrawPolygon(dbg._lineBatch, verts.ToList(), color);
            }
        }
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

    static Vector3[] PlaneToVerts(Vector3 normal, float distance)
    {
        // Define the size of the rectangle to draw
        float halfWidth = 1;
        float halfHeight = 1;

        // Find the closest point on the plane to the origin
        Vector3 closestPoint = normal * distance;

        // Find two orthogonal vectors on the plane
        Vector3 v0;
        if (normal.X != 0)
            v0 = new Vector3(0, 1, 0);
        else if (normal.Y != 0)
            v0 = new Vector3(1, 0, 0);
        else
            v0 = new Vector3(1, 0, 0);

        Vector3 v1 = Vector3.Cross(normal, v0);
        Vector3 v2 = Vector3.Cross(normal, v1);

        // Scale the vectors by halfWidth and halfHeight
        v1 *= halfWidth / v1.Length;
        v2 *= halfHeight / v2.Length;

        // Find the four vertices of the rectangle
        Vector3[] vertices = new Vector3[4];
        vertices[0] = closestPoint + v1 + v2;
        vertices[1] = closestPoint + v1 - v2;
        vertices[2] = closestPoint - v1 - v2;
        vertices[3] = closestPoint - v1 + v2;

        return vertices;
    }

    public static void DrawPolygon(LineBatch lines, List<Vector3> vertices, Color4 color)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 start = vertices[i];
            Vector3 end = vertices[(i + 1) % vertices.Count];
            lines.Append(start, end, color);
        }
    }

    public static Vector3[] ClipPlaneToPolygon(Vector3 boxMin, Vector3 boxMax, Vector3 normal, float distance)
    {
        // Calculate the half extents of the box
        Vector3 halfExtents = (boxMax - boxMin) * 0.5f;
        Vector3 center = boxMin + halfExtents;

        // Check if the plane is parallel to one of the faces of the box
        for (int i = 0; i < 3; i++)
        {
            if (Math.Abs(normal[i]) == 1)
            {
                float d = Vector3.Dot(center, normal) - distance;
                if (Math.Abs(Math.Abs(d) - halfExtents[i]) < 1e-6f || Math.Abs(d) < halfExtents[i])
                {
                    Vector3[] faceVertices = new Vector3[4];
                    faceVertices[0] = center;
                    faceVertices[1] = center;
                    faceVertices[2] = center;
                    faceVertices[3] = center;
                    faceVertices[0][i] += Math.Sign(normal[i]) * halfExtents[i];
                    faceVertices[1][i] += Math.Sign(normal[i]) * halfExtents[i];
                    faceVertices[2][i] += Math.Sign(normal[i]) * halfExtents[i];
                    faceVertices[3][i] += Math.Sign(normal[i]) * halfExtents[i];
                    int i1 = (i + 1) % 3;
                    int i2 = (i + 2) % 3;
                    faceVertices[0][i1] += halfExtents[i1];
                    faceVertices[1][i1] -= halfExtents[i1];
                    faceVertices[2][i1] -= halfExtents[i1];
                    faceVertices[3][i1] += halfExtents[i1];
                    faceVertices[0][i2] += halfExtents[i2];
                    faceVertices[1][i2] += halfExtents[i2];
                    faceVertices[2][i2] -= halfExtents[i2];
                    faceVertices[3][i2] -= halfExtents[i2];
                    return faceVertices;
                }
            }
        }

        // Find the intersection points of the plane with the edges of the box
        List<Vector3> intersectionPointsList = new List<Vector3>();
        Vector3[] boxVertices = new Vector3[8];
        boxVertices[0] = new Vector3(boxMin.X, boxMin.Y, boxMin.Z);
        boxVertices[1] = new Vector3(boxMax.X, boxMin.Y, boxMin.Z);
        boxVertices[2] = new Vector3(boxMax.X, boxMax.Y, boxMin.Z);
        boxVertices[3] = new Vector3(boxMin.X, boxMax.Y, boxMin.Z);
        boxVertices[4] = new Vector3(boxMin.X, boxMin.Y, boxMax.Z);
        boxVertices[5] = new Vector3(boxMax.X, boxMin.Y, boxMax.Z);
        boxVertices[6] = new Vector3(boxMax.X, boxMax.Y, boxMax.Z);
        boxVertices[7] = new Vector3(boxMin.X, boxMax.Y, boxMax.Z);
        int[,] boxEdges = new int[,] { { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 }, { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 }, { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } };
        for (int i = 0; i < 12; i++)
        {
            Vector3 edgeStart = boxVertices[boxEdges[i, 0]];
            Vector3 edgeEnd = boxVertices[boxEdges[i, 1]];
            Vector3 edgeDirection = edgeEnd - edgeStart;
            float denominator = Vector3.Dot(normal, edgeDirection);
            if (Math.Abs(denominator) > float.Epsilon)
            {
                float t = (-distance - Vector3.Dot(normal, edgeStart)) / denominator;
                if (t >= 0 && t <= 1)
                {
                    Vector3 intersectionPoint = edgeStart + t * edgeDirection;
                    intersectionPointsList.Add(intersectionPoint);
                }
            }
        }

        List<Vector3> sortedIntersectionPointsList = SortVertices(intersectionPointsList, normal);

        // Return the sorted intersection points as an array
        return sortedIntersectionPointsList.ToArray();
    }

    public static List<Vector3> SortVertices(List<Vector3> vertices, Vector3 normal)
    {
        // Calculate the center of the vertices
        Vector3 center = Vector3.Zero;
        foreach (Vector3 vertex in vertices)
        {
            center += vertex;
        }
        center /= vertices.Count;

        // Project the vertices onto a plane perpendicular to the normal
        Vector3 planeTangent = Vector3.Cross(normal, new Vector3(1f, 0f, 0f));
        if (planeTangent.LengthSquared < 1e-6f)
        {
            planeTangent = Vector3.Cross(normal, new Vector3(0f, 1f, 0f));
        }
        planeTangent.Normalize();
        Vector3 planeBitangent = Vector3.Cross(normal, planeTangent);

        // Calculate the 2D positions of the projected vertices
        List<Vector2> projectedVertices = new List<Vector2>();
        foreach (Vector3 vertex in vertices)
        {
            Vector2 projectedVertex = new Vector2(Vector3.Dot(vertex - center, planeTangent), Vector3.Dot(vertex - center, planeBitangent));
            projectedVertices.Add(projectedVertex);
        }

        // Sort the projected vertices in clockwise order around their center
        projectedVertices.Sort((a, b) =>
        {
            float angleA = MathF.Atan2(a.Y, a.X);
            float angleB = MathF.Atan2(b.Y, b.X);
            return angleA.CompareTo(angleB);
        });

        // Create a new list of sorted vertices
        List<Vector3> sortedVertices = new List<Vector3>();
        foreach (Vector2 projectedVertex in projectedVertices)
        {
            Vector3 vertex = center + projectedVertex.X * planeTangent + projectedVertex.Y * planeBitangent;
            sortedVertices.Add(vertex);
        }

        return sortedVertices;
    }
}
