namespace GfxEditor;

public interface IModelDrawer
{
    void OnLoad();
    void OnUnload();
    void OnResize(int width, int height);
    void OnRenderFrame();
}