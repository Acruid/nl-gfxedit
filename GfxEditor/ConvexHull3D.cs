using MIConvexHull;
using OpenTK.Mathematics;

namespace GfxEditor
{
    internal static class ConvexHull3D
    {
        // Define a class to represent a vertex
        public class MyVertex : IVertex
        {
            public double[] Position { get; set; }

            public MyVertex(double x, double y, double z)
            {
                Position = new double[] { x, y, z };
            }
        }

        public static List<(Vector3 position, Vector3 normal)> CreateConvexHull(List<Vector3> vertices)
        {
            var hVerts = vertices
                .Select(v => new MyVertex(v.X, v.Y, v.Z))
                .ToList();

            // Compute the convex hull
            var convexHull = ConvexHull.Create<MyVertex, DefaultConvexFace<MyVertex>>(hVerts);

            // Get the list of points
            var points = convexHull.Result.Faces
                .SelectMany(FlatShadeNormals)
                .ToList();

            return points;
        }

        private static IEnumerable<(Vector3 position, Vector3 normal)> FlatShadeNormals(DefaultConvexFace<MyVertex> face)
        {
            var normal = new Vector3((float)face.Normal[0], (float)face.Normal[1], (float)face.Normal[2]);
            for (var i = 0; i < 3; i++)
            {
                var faceVertex = face.Vertices[i];
                var vertex = new Vector3((float)faceVertex.Position[0], (float)faceVertex.Position[1], (float)faceVertex.Position[2]);
                yield return (vertex, normal);
            }
        }
    }
}
