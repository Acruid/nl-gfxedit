using OpenTK.Mathematics;

namespace GfxEditor.Tests;

public class YourClass
{
    [Test]
    public void TestPlanesToVertices()
    {
        // Define the 6 planes of a unit cube
        List<(Vector3 normal, float distance)> planes = new List<(Vector3 normal, float distance)>
        {
            (new Vector3(1, 0, 0), 0.5f),
            (new Vector3(-1, 0, 0), 0.5f),
            (new Vector3(0, 1, 0), 0.5f),
            (new Vector3(0, -1, 0), 0.5f),
            (new Vector3(0, 0, 1), 0.5f),
            (new Vector3(0, 0, -1), 0.5f)
        };

        // Call the PlanesToVertices method
        List<Vector3> result = SceneRenderPresenter.PlanesToVertices(planes);

        // Define the expected result: the 8 vertices of a unit cube
        List<Vector3> expectedResult = new List<Vector3>
        {
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, 0.5f)
        };

        // Assert that the result is equal to the expected result
        foreach (var vertex in expectedResult)
        {
            CollectionAssert.Contains(result, vertex);
        }
    }
}