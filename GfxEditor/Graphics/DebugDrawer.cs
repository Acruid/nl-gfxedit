using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using Color = OpenTK.Mathematics.Color4;

namespace Engine.Graphics
{
    public class DebugDrawer
    {
        private ICamera _camera;
        public LineBatch _lineBatch;
        private ShaderProgram _program;

        public DebugDrawer(ICamera camera)
        {
            _camera = camera;
        }

        public void Initialize()
        {
            _lineBatch = new LineBatch();

            // Setup shader
            {
                _program = new ShaderProgram();
                _program.Add(new Shader(ShaderType.VertexShader, DebugVertexShader));
                _program.Add(new Shader(ShaderType.FragmentShader, DebugFragmentShader));
                _program.Compile();

                _program.Use();
            }
        }

        public void Resize() { }

        public void Update(TimeSpan frameTime)
        {
            //_lineBatch.Append(Vector3.Zero, Vector3.UnitX, Color.Red);
            //_lineBatch.Append(Vector3.Zero, Vector3.UnitY, Color.Green);
            //_lineBatch.Append(Vector3.Zero, Vector3.UnitZ, Color.Blue);


            // apply matrix to the shader
            // OPENTK MATRICES ARE ROW MAJOR, NOT COLUMN MAJOR, MULTIPLY THEM PROPERLY
            // GLSL:   MVP = P * V * M
            // OpenTK: MVP = M * V * P

            var modelMatrix = Matrix4.Identity;
            var mvpMatrix = modelMatrix * _camera.ViewMatrix * _camera.ProjectionMatrix;

            _program.SetUniformMatrix4("transform", false, ref mvpMatrix);
        }

        /// <inheritdoc />
        public void Render()
        {
            _lineBatch.Draw();
        }

        /// <inheritdoc />
        public void Destroy() { }

        #region Shader Source

        public static readonly string DebugVertexShader = @"
#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec4 aColor;

out vec4 vColor;

uniform mat4 transform;

void main()
{
    vColor = aColor;
    gl_Position = transform * vec4(aPos, 1.0);
}
";

        public static readonly string DebugFragmentShader = @"
#version 330 core
in vec4 vColor;

out vec4 FragColor;

void main()
{
    FragColor = vColor;
}
";

        #endregion
    }
}
