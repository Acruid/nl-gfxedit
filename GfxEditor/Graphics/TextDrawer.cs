using Engine.Graphics;

namespace GfxEditor.Graphics
{
    internal class TextDrawer
    {
        private readonly ICamera camera;

        public TextDrawer(ICamera camera)
        {
            this.camera = camera;
        }

        public void Initialize() { }
        public void Resize() { }
        public void Update(TimeSpan frameTime) { }
        public void Render() { }
        public void Destroy() { }
    }
}
