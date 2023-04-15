using System;

namespace OpenTK.Mathematics
{
    public static class Vector2Ex
    {
        public static Vector2 Add(this Vector2 v, float val)
        {
            v.X += val;
            v.Y += val;
            return v;
        }

        public static Vector2 Subtract(this Vector2 v, float val)
        {
            v.X -= val;
            v.Y -= val;
            return v;
        }

        public static Vector2 Floored(this Vector2 v)
        {
            v.X = MathF.Floor(v.X);
            v.Y = MathF.Floor(v.Y);
            return v;
        }
    }
}
