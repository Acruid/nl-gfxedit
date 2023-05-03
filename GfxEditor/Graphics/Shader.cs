using System;
using System.IO;
using OpenTK.Graphics.OpenGL;

namespace Engine.Graphics
{
    public class Shader
    {
        public static readonly string DefaultVertexShader = @"
#version 330 core
layout (location = 0) in vec3 aPos;

void main()
{
    gl_Position = vec4(aPos, 1.0);
}
";

        public static readonly string DefaultFragmentShader = @"
#version 330 core
out vec4 FragColor;

void main()
{
    FragColor = vec4(1.0f, 0.5f, 0.2f, 1.0f);
}
";

        public Shader(ShaderType type, FileInfo file)
        {
            string shaderSource;
            using (var sr = new StreamReader(file.OpenRead()))
            {
                shaderSource = sr.ReadToEnd();
                sr.Close();
            }

            Compile(type, shaderSource);
        }

        public Shader(ShaderType type, string shaderSource)
        {
            Compile(type, shaderSource);
        }

        private void Compile(ShaderType type, string shaderSource)
        {
            Handle = GL.CreateShader(type);
            Type = type;
            GL.ShaderSource(Handle, shaderSource);
            GL.CompileShader(Handle);

            int compiled;
            GL.GetShader(Handle, ShaderParameter.CompileStatus, out compiled);
            if (compiled != 1)
                throw new Exception(GL.GetShaderInfoLog(Handle));
        }

        public int Handle { get; private set; }
        public ShaderType Type { get; private set; }

        /// <summary>
        ///     This can be called after a shader is linked to at least 1 program.
        ///     It will mark the shader object for auto deletion when all references are cleaned up.
        /// </summary>
        public void MarkForDelete()
        {
            GL.DeleteShader(Handle);
        }
    }
}