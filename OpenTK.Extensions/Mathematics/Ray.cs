using System;

namespace OpenTK.Mathematics
{
    public readonly struct Ray : IEquatable<Ray>
    {
        public readonly Vector3 Origin;
        public readonly Vector3 Direction;

        public Ray(Vector3 origin, Vector3 direction)
        {
            Origin = origin;
            Direction = direction;
        }

        #region Equality

        public bool Equals(Ray other)
        {
            return Origin.Equals(other.Origin) && Direction.Equals(other.Direction);
        }

        public override bool Equals(object obj)
        {
            return obj is Ray other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Origin, Direction);
        }

        public static bool operator ==(Ray left, Ray right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Ray left, Ray right)
        {
            return !left.Equals(right);
        }

        #endregion

        public Vector3 Pos(float t)
        {
            var dx = MathF.FusedMultiplyAdd(Direction.X, t, Origin.X);
            var dy = MathF.FusedMultiplyAdd(Direction.Y, t, Origin.Y);
            var dz = MathF.FusedMultiplyAdd(Direction.Z, t, Origin.Z);

            return new Vector3(dx, dy, dz);
        }

        public static Ray FromTwoPoints(Vector3 start, Vector3 end)
        {
            return new Ray(start, (end - start).Normalized());
        }
    }
}
