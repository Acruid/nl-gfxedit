using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Engine.Graphics
{
    public class ShaderProgram : IDisposable
    {
        private readonly Dictionary<string, int> _uniformCache = new Dictionary<string, int>();
        private Shader _fragmentShader;
        private int _handle = -1;
        private Shader _vertexShader;

        public void Add(Shader shader)
        {
            if (_handle != -1)
                throw new InvalidOperationException("Program was already compiled!");

            _uniformCache.Clear();
            switch (shader.Type)
            {
                case ShaderType.VertexShader:
                    _vertexShader = shader;
                    break;
                case ShaderType.FragmentShader:
                    _fragmentShader = shader;
                    break;
                default:
                    throw new NotSupportedException("Tried to add unsupported shader type!");
            }
        }

        public void Compile()
        {
            if(_handle != -1)
                throw new InvalidOperationException("Program was already compiled!");

            _uniformCache.Clear();
            _handle = GL.CreateProgram();

            if (_vertexShader != null)
                GL.AttachShader(_handle, _vertexShader.Handle);

            if (_fragmentShader != null)
                GL.AttachShader(_handle, _fragmentShader.Handle);

            GL.LinkProgram(_handle);

            int compiled;
            GL.GetProgram(_handle, GetProgramParameterName.LinkStatus, out compiled);
            if (compiled != 1)
                throw new Exception(GL.GetProgramInfoLog(_handle));

            // don't need the shaders anymore, they are compiled into the program
            if (_vertexShader != null)
            {
                GL.DeleteShader(_vertexShader.Handle);
                _vertexShader = null;
            }

            if (_fragmentShader != null)
            {
                GL.DeleteShader(_fragmentShader.Handle);
                _fragmentShader = null;
            }

        }

        public void Use()
        {
            if (_handle != -1)
                GL.UseProgram(_handle);
            else
                throw new Exception("Shader has no valid handle!");
        }

        public void Delete()
        {
            if(_handle == -1)
                return;

            GL.UseProgram(0);
            GL.DeleteProgram(_handle);
            _handle = -1;
        }

        public int GetUniform(string name)
        {
            if (_handle == -1)
                throw new Exception("Shader has no valid handle!");

            int result;
            if (_uniformCache.TryGetValue(name, out result))
                return result;

            result = GL.GetUniformLocation(_handle, name);
            if (result == -1)
                throw new Exception("Could not get uniform!");
            _uniformCache.Add(name, result);
            return result;
        }

        public void SetUniformMatrix4(string uniformName, bool transpose, ref Matrix4 matrix)
        {
            Use();
            var uniformId = GetUniform(uniformName);
            GL.UniformMatrix4(uniformId, transpose, ref matrix);
        }

        public void SetUniformVec4(string uniformName, ref Vector4 vector)
        {
            Use();
            var uniformId = GetUniform(uniformName);
            GL.Uniform4(uniformId, vector);
        }

        public void SetUniformVec3(string uniformName, ref Vector3 vector)
        {
            Use();
            var uniformId = GetUniform(uniformName);
            GL.Uniform3(uniformId, vector);
        }

        public void SetUniformTexture(string uniformName, TextureUnit textureUnit)
        {
            Use();
            var uniformId = GetUniform(uniformName);
            GL.Uniform1(uniformId, (textureUnit - TextureUnit.Texture0));
        }

        private void ReleaseUnmanagedResources()
        {
            Delete();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~ShaderProgram() {
            ReleaseUnmanagedResources();
        }
    }
}