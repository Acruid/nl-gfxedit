using OpenTK.Mathematics;
using System.Runtime.InteropServices;

using Vertex = GfxEditor.TriangleDrawer.VertexTex;

namespace GfxEditor;


[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct Triangle
{
    public Vertex Vertex1;
    public Vertex Vertex2;
    public Vertex Vertex3;

    public bool IsTransparent => Vertex1.Color.A != 1 && Vertex2.Color.A != 1 && Vertex3.Color.A != 1;

    public float AverageDepth(Matrix4 viewMatrix)
    {
        // Transform the vertices from world space to view space
        Vector4 v1 = new Vector4(Vertex1.Position, 1) * viewMatrix;
        Vector4 v2 = new Vector4(Vertex2.Position, 1) * viewMatrix;
        Vector4 v3 = new Vector4(Vertex3.Position, 1) * viewMatrix;

        // Calculate the average z-value of the transformed vertices
        return (v1.Z + v2.Z + v3.Z) / 3.0f;
    }

    public override string ToString()
    {
        return $"Alpha: {IsTransparent}";
    }
}

internal static class TriangleSorter
{
    public static int SortTriangles(Span<Vertex> vertices, Matrix4 viewMatrix)
    {
        // Cast the Span<Vertex> to a Span<Triangle>
        Span<Triangle> triangles = MemoryMarshal.Cast<Vertex, Triangle>(vertices);

        // Partition the span into two sections: one for opaque triangles and one for transparent triangles
        var pivotIndex = Partition(triangles);

        // Sort the opaque triangles based on their depth (z-value) in ascending order
        SortByDepth(triangles.Slice(0, pivotIndex), viewMatrix);

        // Sort the transparent triangles based on their depth (z-value) in descending order
        SortByDepth(triangles.Slice(pivotIndex), viewMatrix, true);

        return pivotIndex * 3;
    }

    private static int Partition(Span<Triangle> triangles)
    {
        var pivotIndex = 0;
        for (var i = 0; i < triangles.Length; i++)
            if (!triangles[i].IsTransparent)
            {
                Swap(ref triangles[i], ref triangles[pivotIndex]);
                pivotIndex++;
            }

        return pivotIndex;
    }

    private static void SortByDepth(Span<Triangle> triangles, Matrix4 viewMatrix, bool descending = false)
    {
        triangles.Sort((t1, t2) =>
        {
            var depth1 = t1.AverageDepth(viewMatrix);
            var depth2 = t2.AverageDepth(viewMatrix);
            return descending ? depth2.CompareTo(depth1) : depth1.CompareTo(depth2);
        });
    }

    private static void Swap(ref Triangle a, ref Triangle b)
    {
        (a, b) = (b, a);
    }
}