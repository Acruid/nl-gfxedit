namespace OpenTK.Mathematics;

public static class NumericsQuaternion
{
    public static OpenTK.Mathematics.Quaternion ToTk(this System.Numerics.Quaternion q)
    {
        return new OpenTK.Mathematics.Quaternion(q.X, q.Y, q.Z, q.W);
    }

    public static System.Numerics.Quaternion ToNumeric(this OpenTK.Mathematics.Quaternion q)
    {
        return new System.Numerics.Quaternion(q.X, q.Y, q.Z, q.W);
    }
}