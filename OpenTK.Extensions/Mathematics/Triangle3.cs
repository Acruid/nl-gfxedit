using System;
using System.Runtime.InteropServices;

namespace OpenTK.Mathematics;

/// <summary>
/// A 3d triangle.
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Triangle3 : IEquatable<Triangle3>
{
    /// <summary>
    /// The first point.
    /// </summary>
    public Vector3 A;

    /// <summary>
    /// The second point.
    /// </summary>
    public Vector3 B;

    /// <summary>
    /// The third point.
    /// </summary>
    public Vector3 C;

    #region Equality

    public bool Equals(Triangle3 other)
    {
        return A.Equals(other.A) && B.Equals(other.B) && C.Equals(other.C);
    }

    public override bool Equals(object obj)
    {
        return obj is Triangle3 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(A, B, C);
    }

    public static bool operator ==(Triangle3 left, Triangle3 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Triangle3 left, Triangle3 right)
    {
        return !left.Equals(right);
    }

    #endregion

    public void Transform(in Matrix4 matrix)
    {
        Vector3Ex.Transform(ref A, in matrix);
        Vector3Ex.Transform(ref B, in matrix);
        Vector3Ex.Transform(ref C, in matrix);
    }
}