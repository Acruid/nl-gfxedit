using System;

namespace OpenTK.Mathematics
{
    public static class Matrix3Ex
    {
        public static Matrix3 CreateScale2d(float scale)
        {
            return CreateScale2d(scale, scale);
        }

        public static Matrix3 CreateScale2d(Vector2 scale)
        {
            return CreateScale2d(scale.X, scale.Y);
        }

        public static Matrix3 CreateScale2d(float x, float y)
        {
            return new Matrix3(
                x, 0, 0,  // X Axis
                0, y, 0,  // Y Axis
                0, 0, 1); // Z Axis
        }

        public static Matrix3 CreateRotation2d(float angle)
        {
            var (s, c) = MathF.SinCos(angle);

            return new Matrix3(
                 c, s, 0,  // X Axis
                -s, c, 0,  // Y Axis
                 0, 0, 1); // Z Axis
        }

        public static Matrix3 CreateTranslation2d(float x, float y)
        {
            return new Matrix3(
                1, 0, 0,  // X Axis
                0, 1, 0,  // Y Axis
                x, y, 1); // Z Axis
        }

        public static Matrix3 CreateTransformRight(float x, float y, float angle)
        {
            // Equivalent to Translation * Rotation

            var (sin, cos) = MathF.SinCos(angle);

            Matrix3 combined;

            combined.Row0.X = cos;
            combined.Row0.Y = sin;
            combined.Row0.Z = 0;
            combined.Row1.X = -sin;
            combined.Row1.Y = cos;
            combined.Row1.Z = 0;
            combined.Row2.X = x * cos + y * -sin;
            combined.Row2.Y = x * sin + y * cos;
            combined.Row2.Z = 1;

            return combined;
        }

        public static Matrix3 CreateTransformLeft(float x, float y, float angle)
        {
            // Equivalent to Rotation * Translation

            var rot = CreateRotation2d(angle);
            var trans = CreateTranslation2d(x, y);

            return Matrix3.Mult(rot, trans);
        }

        
        public static Matrix3 CreateTransformLeft(float x, float y, float angle, float sx, float sy)
        {
            // Equivalent to Scale * Rotation * Translation

            var scale = CreateScale2d(sx, sy);
            var rot = CreateRotation2d(angle);
            var trans = CreateTranslation2d(x, y);

            return scale * rot * trans;
        }

        public static Matrix3 CreateInverseTransform(float x, float y, float angle)
        {
            var xform = CreateTransformLeft(x, y, angle);
            return Matrix3.Invert(xform);
        }

        public static Vector2 TransformVec(Vector2 vec, Matrix3 mat)
        {
            var vecHomo = new Vector3(vec.X, vec.Y, 1);
            vecHomo *= mat;
            return vecHomo.Xy;
        }
    }
}
