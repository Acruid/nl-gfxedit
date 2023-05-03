using System;
using OpenTK.Mathematics;

namespace Engine.Graphics
{
    /// <inheritdoc />
    public class Camera : ICamera
    {
        /// <inheritdoc />
        public float FieldOfView { get; set; } = 60f;

        /// <inheritdoc />
        public float NearPlane { get; set; } = 0.01f; // 1cm

        /// <inheritdoc />
        public float FarPlane { get; set; } = 1000.0f; // 1km

        /// <inheritdoc />
        public Matrix4 ProjectionMatrix
        {
            get
            {
                var aspectRatio = (float) ViewportSize.X / ViewportSize.Y;

                var projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FieldOfView),
                    aspectRatio,
                    NearPlane, // 1cm
                    FarPlane // 1km
                );

                // By default the created perspective projection uses -Z as the forward vector, and +Y as up.
                // This adds a rotation so that +X is forward and +Z is up.
                //var basisQuaternion = Quaternion.FromEulerAngles(MathHelper.DegreesToRadians(-90), MathHelper.DegreesToRadians(0), MathHelper.DegreesToRadians(90));
                //var basisMatrix = Matrix4.CreateFromQuaternion(basisQuaternion);

                //return basisMatrix * projectionMatrix;

                return projectionMatrix;
            }
        }

        /// <inheritdoc />
        public Transform3D Transform { get; } = new();

        /// <inheritdoc />
        public Matrix4 ViewMatrix => Transform.InvMatrix;

        /// <inheritdoc />
        public Vector2i ViewportSize { get; set; }

        /// <inheritdoc />
        public Vector3 WorldUp => Vector3.UnitZ;

        /// <inheritdoc />
        public Ray UnProject(Vector2i screenPos)
        {
            // these mouse.Z values are NOT scientific.
            // Near plane needs to be < -1.5f or we have trouble selecting objects right in front of the camera. (why?)
            var pos1 = UnProject(ProjectionMatrix, ViewMatrix, ViewportSize,
                new Vector3(screenPos.X, screenPos.Y, -1.5f)); // near
            var pos2 = UnProject(ProjectionMatrix, ViewMatrix, ViewportSize,
                new Vector3(screenPos.X, screenPos.Y, 1.0f)); // far
            return Ray.FromTwoPoints(pos1, pos2);
        }

        /// <inheritdoc />
        public Vector2i Project(Vector3 worldPos)
        {
            // http://www.songho.ca/opengl/gl_transform.html

            var vec = new Vector4(worldPos, 1);
            Vector4.TransformRow(in vec, ViewMatrix, out vec); // Eye Coords
            Vector4.TransformRow(in vec, ProjectionMatrix, out vec); // Clip coords

            // clip space to ndc space, "projection divide"
            if (vec.W > float.Epsilon || vec.W < -float.Epsilon)
            {
                vec.X /= vec.W;
                vec.Y /= vec.W;
                vec.Z /= vec.W;
            }

            // vec is now in the "default" coordinate space of OpenGL
            var vpX = 0;
            var vpY = 0;
            var vpWidth = ViewportSize.X;
            var vpHeight = ViewportSize.Y;

            // undo the GL.Viewport, going from ndc to window pixel coords.
            Vector2 window;
            window.X = vpX + vpWidth * (vec.X + 1) / 2;
            window.Y = vpY + vpHeight * (vec.Y + 1) / 2;
            // Z is depth info

            return new Vector2i((int)MathF.Round(window.X), (int)MathF.Round(window.Y));
        }

        // UnProject takes a window-local mouse-coordinate, and a Z-coordinate depth [0,1] and 
        // unprojects it, returning the point in world space. To get a ray, UnProject the
        // mouse coordinates at two different z-values.
        //
        // http://www.opentk.com/node/1276#comment-13029

        private static Vector3 UnProject(in Matrix4 projection, in Matrix4 view, in Vector2i viewportSize, in Vector3 mousePos)
        {
            Vector4 vec;

            vec.X = 2.0f * mousePos.X / viewportSize.X - 1;
            vec.Y = -(2.0f * mousePos.Y / viewportSize.Y - 1);
            vec.Z = mousePos.Z;
            vec.W = 1.0f;

            var viewInv = Matrix4.Invert(view);
            var projInv = Matrix4.Invert(projection);

            Vector4.TransformRow(in vec, in projInv, out vec);
            Vector4.TransformRow(in vec, in viewInv, out vec);

            if (vec.W > float.Epsilon || vec.W < -float.Epsilon)
            {
                vec.X /= vec.W;
                vec.Y /= vec.W;
                vec.Z /= vec.W;
            }

            return new Vector3(vec.X, vec.Y, vec.Z);
        }
    }
}