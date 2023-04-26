using OpenTK.Windowing.Common;

namespace GfxEditor;

public interface IModelDrawer
{
    void OnLoad();
    void OnUnload();
    void OnResize(int width, int height);
    void OnRenderFrame();
    void HandleKeyDown(KeyboardKeyEventArgs args);
    void HandleKeyUp(KeyboardKeyEventArgs args);
    void HandleMouseDown(MouseButtonEventArgs args);
    void HandleMouseUp(MouseButtonEventArgs args);
    void HandleMouseWheel(MouseWheelEventArgs args);
    void HandleText(TextInputEventArgs args);
    void PresentUi();
}