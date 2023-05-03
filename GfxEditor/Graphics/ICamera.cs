using OpenTK.Mathematics;

namespace Engine.Graphics
{
    /// <summary>
    /// Represents a 3D camera inside the world.
    /// </summary>
    public interface ICamera
    {
        /// <summary>
        /// Current camera position and orientation in the world.
        /// </summary>
        Transform3D Transform { get; }

        /// <summary>
        /// Vertical FOV of the camera, in degrees.
        /// </summary>
        /// <remarks>
        /// 0 &lt; FOV &lt; Math.PI
        /// </remarks>
        float FieldOfView { get; set; }

        /// <summary>
        /// Near plane of the viewing frustum.
        /// </summary>
        float NearPlane { get; set; }

        /// <summary>
        /// Far plane of the viewing frustum.
        /// </summary>
        float FarPlane { get; set; }

        /// <summary>
        /// The basis vector that is considered "up" in the world, which is the opposite of gravity.
        /// </summary>
        Vector3 WorldUp { get; }

        /// <summary>
        /// View matrix of the camera.
        /// </summary>
        Matrix4 ViewMatrix { get; }

        /// <summary>
        /// Projection matrix of the camera, used to project the 3D scene onto the 2D viewport.
        /// </summary>
        Matrix4 ProjectionMatrix { get; }

        /// <summary>
        /// The size of the 2D viewport, in pixels. This is the size of the client area that is being drawn to in the window.
        /// </summary>
        Vector2i ViewportSize { get; set; }

        /// <summary>
        /// UnProjects a point on the screen into a ray inside the view frustum.
        /// </summary>
        /// <param name="screenPos">Position on the screen in pixels.</param>
        /// <returns>A ray with the origin on the near plane, that </returns>
        Ray UnProject(Vector2i screenPos);

        /// <summary>
        /// Projects a point from the world onto the screen.
        /// </summary>
        /// <param name="worldPos">Position in the world to project onto the screen, in meters.</param>
        /// <returns>Position on the screen, in pixels.</returns>
        Vector2i Project(Vector3 worldPos);
    }
}