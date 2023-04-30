using ImGuiNET;

namespace GfxEditor;

internal class LodDataPresenter
{
    private readonly Window _parent;
    private readonly GfxEdit _model;

    public LodDataPresenter(Window parent, GfxEdit model)
    {
        _parent = parent;
        _model = model;
    }

    public void Present()
    {
        ImGui.Begin("Gfx Lod Data");

        //TODO: LOD Data!

        ImGui.End();
    }
}