
using System;

namespace OpenTK.Mathematics
{
    public static class QuaternionHelpers
    {
        public static (float qx, float qy, float qz, float qw) euler_to_quaternion(float pitch, float yaw, float roll)
        {
            float qx = MathF.Sin(roll / 2) * MathF.Cos(pitch / 2) * MathF.Cos(yaw / 2) - MathF.Cos(roll / 2) * MathF.Sin(pitch / 2) * MathF.Sin(yaw / 2);
            float qy = MathF.Cos(roll / 2) * MathF.Sin(pitch / 2) * MathF.Cos(yaw / 2) + MathF.Sin(roll / 2) * MathF.Cos(pitch / 2) * MathF.Sin(yaw / 2);
            float qz = MathF.Cos(roll / 2) * MathF.Cos(pitch / 2) * MathF.Sin(yaw / 2) - MathF.Sin(roll / 2) * MathF.Sin(pitch / 2) * MathF.Cos(yaw / 2);
            float qw = MathF.Cos(roll / 2) * MathF.Cos(pitch / 2) * MathF.Cos(yaw / 2) + MathF.Sin(roll / 2) * MathF.Sin(pitch / 2) * MathF.Sin(yaw / 2);

            return (qx, qy, qz, qw);
        }

        public static (float pitch, float yaw, float roll) quaternion_to_euler(float x, float y, float z, float w)
        {
            float t0 = +2.0f * (w * x + y * z);
            float t1 = +1.0f - 2.0f * (x * x + y * y);
            float roll = MathF.Atan2(t0, t1);
            float t2 = +2.0f * (w * y - z * x);
            t2 = t2 > +1.0 ? +1.0f : t2;
            t2 = t2 < -1.0 ? -1.0f : t2;
            float pitch = MathF.Asin(t2);
            float t3 = +2.0f * (w * z + x * y);
            float t4 = +1.0f - 2.0f * (y * y + z * z);
            float yaw = MathF.Atan2(t3, t4);
            return (pitch, yaw, roll);
        }

        // Returns a quaternion such that q*start = dest
        public static Quaternion RotationBetweenVectors(Vector3 start, Vector3 dest)
        {
            //http://www.opengl-tutorial.org/intermediate-tutorials/tutorial-17-quaternions/

            start = Vector3.Normalize(start);
            dest = Vector3.Normalize(dest);

            var cosTheta = Vector3.Dot(start, dest);
            Vector3 rotationAxis;

            if (cosTheta < -1 + 0.001f)
            {
                // special case when vectors in opposite directions:
                // there is no "ideal" rotation axis
                // So guess one; any will do as long as it's perpendicular to start
                rotationAxis = Vector3.Cross(Vector3.UnitY, start);
                if (rotationAxis.LengthSquared < 0.01) // bad luck, they were parallel, try again!
                    rotationAxis = Vector3.Cross(Vector3.UnitX, start);

                rotationAxis = Vector3.Normalize(rotationAxis);
                return new Quaternion(rotationAxis, MathHelper.DegreesToRadians(180.0f));
            }

            rotationAxis = Vector3.Cross(start, dest);

            var s = MathF.Sqrt((1 + cosTheta) * 2);
            var invs = 1 / s;

            return new Quaternion(
                rotationAxis.X * invs,
                rotationAxis.Y * invs,
                rotationAxis.Z * invs,
                s * 0.5f
            );
        }
    }
}
