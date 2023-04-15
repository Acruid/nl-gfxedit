namespace OpenTK.Mathematics
{
    public static class Vector3Ex
    {
        public static void Transform(this ref Vector3 vec, in Matrix4 mat)
        {
            vec = (new Vector4(vec, 1) * mat).Xyz;
        }

        public static Vector3 Transformed(this Vector3 vec, in Matrix4 mat)
        {
            return (new Vector4(vec, 1) * mat).Xyz;
        }
    }
}
