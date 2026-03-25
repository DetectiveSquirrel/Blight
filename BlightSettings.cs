using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using Newtonsoft.Json;
using SharpDX;
using static System.Enum;

namespace Blight;

public static class ColorExtensions
{
    public static Color ToSharpDx(this System.Drawing.Color color) => new(color.R, color.G, color.B, color.A);
}

public class BlightSettings : ISettings
{
    public ToggleNode DisableDrawOnLeftOrRightPanelsOpen { get; set; } = new(false);
    public ToggleNode IgnoreFullscreenPanels { get; set; } = new(false);
    public ToggleNode IgnoreLargePanels { get; set; } = new(false);

    public Pathway Pathways { get; set; } = new();
    public TowerList Towers { get; set; } = new();
    public ToggleNode Enable { get; set; } = new(false);
}

[Submenu(CollapsedByDefault = false)]
public class Pathway
{
    public ToggleNode DisableWhenPathwayClosed { get; set; } = new(true);
    public ColorNode MapColor { get; set; } = new Color(255, 255, 255, 111);
    public ColorNode WorldColor { get; set; } = new Color(194, 200, 0, 57);
    public RangeNode<int> MapLineWidth { get; set; } = new(3, 1, 100);
    public RangeNode<int> WorldLineWidth { get; set; } = new(8, 1, 100);
    public ToggleNode DrawMap { get; set; } = new(true);
    public ToggleNode DrawWorld { get; set; } = new(true);
}

[Submenu(CollapsedByDefault = false)]
public class TowerList
{
    public ToggleNode FollowWorldTerrain { get; set; } = new(false);

    public TowerSettings FireTower { get; set; } = new()
    {
        DrawStyle = new TowerDrawStyle
        {
            Ground = new TowerGroundDraw { Color = new ColorNode(new Color(255, 96, 96, 82)) },
            Map = new TowerMapDraw { Color = new ColorNode(new Color(255, 96, 96, 225)) }
        }
    };

    public TowerSettings ColdTower { get; set; } = new()
    {
        DrawStyle = new TowerDrawStyle
        {
            Ground = new TowerGroundDraw { Color = new ColorNode(new Color(150, 210, 255, 82)) },
            Map = new TowerMapDraw { Color = new ColorNode(new Color(150, 210, 255, 225)) }
        }
    };

    public TowerSettings LightningTower { get; set; } = new()
    {
        DrawStyle = new TowerDrawStyle
        {
            Ground = new TowerGroundDraw { Color = new ColorNode(new Color(255, 255, 190, 82)) },
            Map = new TowerMapDraw { Color = new ColorNode(new Color(255, 255, 190, 225)) }
        }
    };

    public TowerSettings PhysicalTower { get; set; } = new()
    {
        DrawStyle = new TowerDrawStyle
        {
            Ground = new TowerGroundDraw { Color = new ColorNode(new Color(200, 140, 96, 82)) },
            Map = new TowerMapDraw { Color = new ColorNode(new Color(200, 140, 96, 225)) }
        }
    };

    public TowerSettings MinionTower { get; set; } = new()
    {
        DrawStyle = new TowerDrawStyle
        {
            Ground = new TowerGroundDraw { Color = new ColorNode(new Color(200, 120, 235, 82)) },
            Map = new TowerMapDraw { Color = new ColorNode(new Color(200, 120, 235, 225)) }
        }
    };

    public TowerSettings BuffTower { get; set; } = new()
    {
        DrawStyle = new TowerDrawStyle
        {
            Ground = new TowerGroundDraw { Color = new ColorNode(new Color(150, 255, 168, 82)) },
            Map = new TowerMapDraw { Color = new ColorNode(new Color(150, 255, 168, 225)) }
        }
    };
}

public enum CircleDrawStyle
{
    Filled,
    Outline
}

[Submenu(CollapsedByDefault = false)]
public class TowerSettings
{
    public TowerDrawStyle DrawStyle { get; set; } = new();
}

[Submenu(CollapsedByDefault = false)]
public class TowerDrawStyle
{
    public TowerGroundDraw Ground { get; set; } = new();
    public TowerMapDraw Map { get; set; } = new();
    public RangeNode<float> CircleThickness { get; set; } = new(10f, 1f, 20f);
}

[Submenu(CollapsedByDefault = true)]
public class TowerGroundDraw
{
    public TowerGroundDraw()
    {
        CircleDrawType.SetListValues([.. GetNames(typeof(CircleDrawStyle))]);
    }

    public ToggleNode Draw { get; set; } = new(true);
    public ListNode CircleDrawType { get; set; } = new() { Value = nameof(CircleDrawStyle.Outline) };
    public ColorNode Color { get; set; } = new(new Color(255, 255, 255, 80));

    [JsonIgnore]
    public CircleDrawStyle Style
    {
        get => (CircleDrawStyle)Parse(typeof(CircleDrawStyle), CircleDrawType.Value);
        set => CircleDrawType.Value = value.ToString();
    }
}

[Submenu(CollapsedByDefault = true)]
public class TowerMapDraw
{
    public TowerMapDraw()
    {
        CircleDrawType.SetListValues([.. GetNames(typeof(CircleDrawStyle))]);
    }

    public ToggleNode Draw { get; set; } = new(false);
    public ListNode CircleDrawType { get; set; } = new() { Value = nameof(CircleDrawStyle.Outline) };
    public ColorNode Color { get; set; } = new(new Color(255, 255, 255, 255));

    [JsonIgnore]
    public CircleDrawStyle Style
    {
        get => (CircleDrawStyle)Parse(typeof(CircleDrawStyle), CircleDrawType.Value);
        set => CircleDrawType.Value = value.ToString();
    }
}