using System.Reflection;
using System.Runtime.InteropServices;

namespace GfxEditor;

public static class ListHelpers
{
    /// <summary>
    /// Get a <see cref="Span{T}"/> view over a <see cref="List{T}"/>'s data.
    /// Items should not be added or removed from the <see cref="List{T}"/> while the <see cref="Span{T}"/> is in use.
    /// </summary>
    /// <param name="list">The list to get the data view over.</param>
    public static Span<T> AsCapacitySpan<T>(this List<T>? list)
        where T : unmanaged
    {
        if (list is null)
            return default;

        var sizeField = typeof(List<T>).GetField("_size", BindingFlags.Instance | BindingFlags.NonPublic);

        if (sizeField is null)
            throw new InvalidOperationException("Could not find internal field _size inside List.");

        var itemField = typeof(List<T>).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic);

        if (itemField is null)
            throw new InvalidOperationException("Could not find internal field _items inside List.");

        if (itemField.GetValue(list) is not T[] items)
            throw new InvalidOperationException("Items retrieved from list is null.");

        sizeField.SetValue(list, items.Length);
        return new Span<T>(items);
    }

    /// <summary>
    /// Get a <see cref="Span{T}"/> view over a <see cref="List{T}"/>'s data.
    /// Items should not be added or removed from the <see cref="List{T}"/> while the <see cref="Span{T}"/> is in use.
    /// </summary>
    /// <param name="list">The list to get the data view over.</param>
    public static Span<T> AsSizeSpan<T>(this List<T>? list)
        where T : unmanaged
    {
        return CollectionsMarshal.AsSpan(list);
    }
}
