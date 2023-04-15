namespace OpenTK.Mathematics
{
    public static class NumericsVector3
    {
        // C# does not support operator helper functions. It is not possible to add operators
        // to a third party type.

        public static System.Numerics.Vector3 ToNumeric(this Vector3 v)
        {
            return new System.Numerics.Vector3(v.X, v.Y, v.Z);
        }

        public static Vector3 ToTk(this System.Numerics.Vector3 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }
    }
}
