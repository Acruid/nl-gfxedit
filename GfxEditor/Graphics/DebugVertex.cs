using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using Color = OpenTK.Mathematics.Color4;

namespace Engine.Graphics
{
    [StructLayout(LayoutKind.Sequential, Pack = 1), Serializable]
    internal readonly struct DebugVertex
    {
        public readonly Vector3 Position;
        public readonly Color Color;

        public DebugVertex(Vector3 position, Color color)
        {
            Position = position;
            Color = color;
        }
    }
}
