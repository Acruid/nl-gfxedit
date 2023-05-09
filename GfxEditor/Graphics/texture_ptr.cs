namespace GfxEditor.Graphics;

public readonly struct texture_ptr : IEquatable<texture_ptr>
{
    private readonly int _value;

    public texture_ptr(int value) => _value = value;
    public static implicit operator int(texture_ptr ptr) => ptr._value;

    #region Equality
    public bool Equals(texture_ptr other) => _value == other._value;
    public override bool Equals(object obj) => obj is texture_ptr other && Equals(other);
    public override int GetHashCode() => _value;
    public static bool operator ==(texture_ptr left, texture_ptr right) => left.Equals(right);
    public static bool operator !=(texture_ptr left, texture_ptr right) => !left.Equals(right);
    #endregion
}