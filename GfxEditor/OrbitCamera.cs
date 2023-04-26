using System;
using OpenTK.Mathematics;

namespace Engine.Graphics
{
    public class ArcballCameraController
    {
        private Vector2 _sphereicalAngles;
        // Credit: https://nerdhut.de/2020/05/09/unity-arcball-camera-spherical-coordinates/

        public ArcballCameraController(ICamera camera)
        {
            Camera = camera;
        }

        public ICamera Camera { get; }
        public Vector3 PivotPoint { get; set; }
        public float SphereRadius { get; set; }

        public Vector2 SphereicalAngles
        {
            get => _sphereicalAngles;
            set
            {
                // -PI <= X <= PI
                // -PI < Y < 0
                _sphereicalAngles = value;
            }
        }

        public float InputScale { get; set; } = 1;

        public void UpdateZoom(float dz)
        {
            SphereRadius += dz * InputScale;
            SphereRadius = MathF.Max(0.001f, SphereRadius);
        }

        public void UpdateAngleInput(float dx, float dy, float dt)
        {
            _sphereicalAngles.X += dx * dt * InputScale;
            _sphereicalAngles.Y += dy * dt * InputScale;

            if (_sphereicalAngles.X > MathF.PI)
                _sphereicalAngles.X -= MathF.PI * 2;
            else if (_sphereicalAngles.X < -MathF.PI)
                _sphereicalAngles.X += MathF.PI * 2;

            _sphereicalAngles.Y = MathF.Max(0.001f, _sphereicalAngles.Y);
            _sphereicalAngles.Y = MathF.Min(MathF.PI - 0.001f, _sphereicalAngles.Y);
        }

        public void FrameSphere(float batchSphereRadius)
        {
            // distance = worldSphereRadius / tan(FOVrad * 0.5)
            var distance = batchSphereRadius / MathF.Tan((Camera.FieldOfView * 0.01745329f) * 0.5f);
            SphereRadius = distance * 1.2f; // 1.2 to add some margin around the scene
        }

        /// <summary>
        /// Calculates camera position and regenerates the view matrix.
        /// </summary>
        public void UpdateView()
        {
            var targetPos = CalcTargetPos(SphereRadius, SphereicalAngles.Y, SphereicalAngles.X);
            var worldTargetPos = targetPos + PivotPoint;
            
            //TODO: Camera ray collision

            Quaternion lookQuat;
            if (SphereRadius == 0)
            {
                lookQuat = Quaternion.Identity;
            }
            else
            {
                var up = Vector3.UnitZ;
                var direction = targetPos;

                Vector3 vector3_1 = Vector3.Normalize(direction);
                Vector3 right = Vector3.Normalize(Vector3.Cross(up, vector3_1));
                Vector3 vector3_2 = Vector3.Normalize(Vector3.Cross(vector3_1, right));
                Matrix3 result;
                result.Row0.X = right.X; //transposed matrix negates direction
                result.Row0.Y = vector3_2.X;
                result.Row0.Z = vector3_1.X;
                result.Row1.X = right.Y;
                result.Row1.Y = vector3_2.Y;
                result.Row1.Z = vector3_1.Y;
                result.Row2.X = right.Z;
                result.Row2.Y = vector3_2.Z;
                result.Row2.Z = vector3_1.Z;

                lookQuat = Quaternion.FromMatrix(result);
            }

            Camera.Transform.Rotation = lookQuat;
            Camera.Transform.Position = worldTargetPos;
        }

        /// <summary>
        /// Spherical coords to cartesian coords.
        /// </summary>
        /// <param name="radius">radius of sphere in meters.</param>
        /// <param name="theta">polar angle in radians.</param>
        /// <param name="phi">azimuthal angle in radians.</param>
        /// <returns>3D cartesian coordinates in local space.</returns>
        private Vector3 CalcTargetPos(float radius, float theta, float phi)
        {
            var x = radius * MathF.Cos(phi) * MathF.Sin(theta);
            var y = radius * MathF.Sin(phi) * MathF.Sin(theta);
            var z = radius * MathF.Cos(theta);

            return new Vector3(x, y, z);
        }
    }
}