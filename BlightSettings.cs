using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace Blight;

public static class ColorExtensions
{
    public static Color ToSharpDx(this System.Drawing.Color color)
    {
        return new Color(color.R, color.G, color.B, color.A);
    }
}

public class BlightSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new(false);
    public ToggleNode DisableDrawOnLeftOrRightPanelsOpen { get; set; } = new(false);
    public ToggleNode IgnoreFullscreenPanels { get; set; } = new(false);
    public ToggleNode IgnoreLargePanels { get; set; } = new(false);

    public Pathway Pathways { get; set; } = new();
    public TowerList Towers { get; set; } = new();
}

[Submenu(CollapsedByDefault = false)]
public class Pathway
{
    public ColorNode MapColor { get; set; } = new Color(255,255,255, 111);
    public ColorNode WorldColor { get; set; } = new Color(194,200, 0, 57);
    public RangeNode<int> MapLineWidth { get; set; } = new(3, 1, 100);
    public RangeNode<int> WorldLineWidth { get; set; } = new(8, 1, 100);
    public ToggleNode DrawMap { get; set; } = new(true);
    public ToggleNode DrawWorld { get; set; } = new(true);
}

[Submenu(CollapsedByDefault = false)]
public class TowerList
{
    public TowerSettings FireTower { get; set; } = new()
    {
        DrawGround = new ToggleNode(true),
        Color = new ColorNode(Color.Red),
        DrawMap = new ToggleNode(false)
    };

    public TowerSettings ColdTower { get; set; } = new()
    {
        DrawGround = new ToggleNode(true),
        Color = new ColorNode(Color.SkyBlue),
        DrawMap = new ToggleNode(false)
    };

    public TowerSettings LightningTower { get; set; } = new()
    {
        DrawGround = new ToggleNode(true),
        Color = new ColorNode(Color.LightYellow),
        DrawMap = new ToggleNode(false)
    };

    public TowerSettings PhysicalTower { get; set; } = new()
    {
        DrawGround = new ToggleNode(true),
        Color = new ColorNode(Color.Brown),
        DrawMap = new ToggleNode(false)
    };

    public TowerSettings MinionTower { get; set; } = new()
    {
        DrawGround = new ToggleNode(true),
        Color = new ColorNode(Color.Purple),
        DrawMap = new ToggleNode(false)
    };

    public TowerSettings BuffTower { get; set; } = new()
    {
        DrawGround = new ToggleNode(true),
        Color = new ColorNode(Color.LimeGreen),
        DrawMap = new ToggleNode(false)
    };
}


[Submenu(CollapsedByDefault = false)]
public class TowerSettings
{
    public ToggleNode DrawGround { get; set; } = new(true);
    public ToggleNode DrawMap { get; set; } = new(false);
    public RangeNode<float> CircleThickness { get; set; } = new(3f, 1f, 15f);
    public ColorNode Color { get; set; } = System.Drawing.Color.FromArgb(255, 255, 255, 255).ToSharpDx();
}