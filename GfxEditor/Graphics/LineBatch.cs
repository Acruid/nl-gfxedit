using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using Color = OpenTK.Mathematics.Color4;
using Rectangle = OpenTK.Mathematics.Box2;

namespace Engine.Graphics
{
    public class LineBatch : IDisposable
    {
        private const int MAX_VERTICES = 0xFFFF;
        private readonly List<DebugVertex> _lines = new List<DebugVertex>();
        private readonly int _stride;
        private vao_ptr _vao;
        private vbo_ptr _vbo;

        public LineBatch()
        {
            // total size of each vertex element in the array
            _stride = Marshal.SizeOf(typeof(DebugVertex));

            // create a VAO and set up a VBO inside of it
            _vao = vao_ptr.GenerateBound("LineBatch");
            GL.BindVertexArray(_vao);
            {
                // create a VBO
                _vbo = vbo_ptr.GenBuffer();

                // binds our VBO to the ArrayBuffer target
                GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);

                // allocate an empty max size buffer
                GL.BufferData(BufferTarget.ArrayBuffer,
                    _stride * MAX_VERTICES,
                    IntPtr.Zero,
                    BufferUsageHint.StreamDraw);

                // set up and enable attribute 0 (aPos)
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, _stride, 0 * sizeof(float));
                GL.EnableVertexAttribArray(0);

                // set up and enable attribute 1 (aColor)
                GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, _stride, 3 * sizeof(float));
                GL.EnableVertexAttribArray(1);
            }
        }

        public void Append(Vector3 startPos, Vector3 endPos, Color color)
        {
            if (_lines.Count + 2 >= MAX_VERTICES)
                return;

            _lines.Add(new DebugVertex(startPos, color));
            _lines.Add(new DebugVertex(endPos, color));
        }

        public void Append(Vector3 position, float scale, Color color)
        {
            Append(position - Vector3.UnitX * scale, position + Vector3.UnitX * scale, color);
            Append(position - Vector3.UnitY * scale, position + Vector3.UnitY * scale, color);
            Append(position - Vector3.UnitZ * scale, position + Vector3.UnitZ * scale, color);
        }

        public void Append(Rectangle rect, Color color)
        {
            if (_lines.Count + 8 >= MAX_VERTICES)
                return;

            _lines.Add(new DebugVertex(new Vector3(rect.Max.X, rect.Max.Y, 0), color));
            _lines.Add(new DebugVertex(new Vector3(rect.Max.X, rect.Min.Y, 0), color));

            _lines.Add(new DebugVertex(new Vector3(rect.Min.X, rect.Max.Y, 0), color));
            _lines.Add(new DebugVertex(new Vector3(rect.Min.X, rect.Min.Y, 0), color));

            _lines.Add(new DebugVertex(new Vector3(rect.Max.X, rect.Max.Y, 0), color));
            _lines.Add(new DebugVertex(new Vector3(rect.Min.X, rect.Max.Y, 0), color));

            _lines.Add(new DebugVertex(new Vector3(rect.Max.X, rect.Min.Y, 0), color));
            _lines.Add(new DebugVertex(new Vector3(rect.Min.X, rect.Min.Y, 0), color));
        }

        public void Draw()
        {
            GL.BindVertexArray(_vao);

            // copies the vertices array to the ArrayBuffer target, with the hint that we will draw this array every frame
            // the first BufferData call signals to the driver to use buffer orphaning to prevent implicit synchronization
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _stride * _lines.Count, _lines.ToArray());

            GL.DrawArrays(PrimitiveType.Lines, 0, _lines.Count);

            _lines.Clear();
        }

        private void ReleaseUnmanagedResources()
        {
            {
                _vbo.Free();
                _vbo = vbo_ptr.Invalid;
                _vao.Free();
                _vao = vao_ptr.Invalid;
            }
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }
    }
}
