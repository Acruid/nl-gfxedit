using System;

namespace OpenTK.Mathematics
{
    public readonly struct Plane : IEquatable<Plane>
    {
        public readonly Vector3 Origin;
        public readonly Vector3 Normal;
        public static readonly Plane Xy = new Plane(Vector3.Zero, Vector3.UnitZ);

        public Plane(Vector3 origin, Vector3 normal)
        {
            Origin = origin;
            Normal = Vector3.Normalize(normal);
        }

        public bool Intersect(in Ray ray, out float t)
        {
            var l0 = ray.Origin;
            var l = ray.Direction;
            var p0 = Origin;
            var n = Normal;

            // assuming vectors are all normalized
            var denom = Vector3.Dot(n, l);

            if (MathF.Abs(denom) > 1e-6) // abs allows rays from both sides of the plane
            {
                t = Vector3.Dot(p0 - l0, n) / denom;
                return t >= 0;
            }

            t = 0;
            return false;
        }

        #region Equality

        public bool Equals(Plane other)
        {
            return Origin.Equals(other.Origin) && Normal.Equals(other.Normal);
        }

        public override bool Equals(object obj)
        {
            return obj is Plane other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Origin, Normal);
        }

        public static bool operator ==(Plane left, Plane right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Plane left, Plane right)
        {
            return !left.Equals(right);
        }

        #endregion
    }
}
