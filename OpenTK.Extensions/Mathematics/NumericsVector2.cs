using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTK.Mathematics
{
    public static class NumericsVector2
    {
        // C# does not support operator helper functions. It is not possible to add operators
        // to a third party type.

        public static System.Numerics.Vector2 ToNumeric(this Vector2 v)
        {
            return new System.Numerics.Vector2(v.X, v.Y);
        }

        public static Vector2 ToTk(this System.Numerics.Vector2 v)
        {
            return new Vector2(v.X, v.Y);
        }
    }
}
