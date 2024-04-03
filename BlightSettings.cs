using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace Blight
{
    public class BlightSettings : ISettings
    {
        //Mandatory setting to allow enabling/disabling your plugin
        public ToggleNode Enable { get; set; } = new ToggleNode(false);
        public ColorNode BlightColorMap { get; set; } = new Color(0, 200, 0, 150);
        public RangeNode<int> BlightWidthMap { get; set; } = new RangeNode<int>(3, 1, 100);
        public ColorNode BlightColorWorld { get; set; } = new Color(0, 200, 0, 150);
        public RangeNode<int> BlightWidthWorld { get; set; } = new RangeNode<int>(8, 1, 100);
        public RangeNode<int> MaxWorldDrawDistance { get; set; } = new RangeNode<int>(160, 1, 600);
        public ToggleNode DrawMap { get; set; } = new ToggleNode(true);
        public ToggleNode DrawWorld { get; set; } = new ToggleNode(false);
        public ToggleNode DisableDrawOnLeftOrRightPanelsOpen { get; set; } = new ToggleNode(false);
        public ToggleNode IgnoreFullscreenPanels { get; set; } = new ToggleNode(false);
        public ToggleNode IgnoreLargePanels { get; set; } = new ToggleNode(false);


        public RangeNode<float> CircleThickness { get; set; } = new RangeNode<float>(3f, 1f, 15f);
        public ColorNode FireTowerColor { get; set; } = new ColorNode(Color.Red);
        public ColorNode ColdTowerColor { get; set; } = new ColorNode(Color.SkyBlue);
        public ColorNode LightningTowerColor { get; set; } = new ColorNode(Color.LightYellow);
        public ColorNode PhysicalTowerColor { get; set; } = new ColorNode(Color.Brown);
        public ColorNode MinionTowerColor { get; set; } = new ColorNode(Color.Purple);
        public ColorNode BuffTowerColor { get; set; } = new ColorNode(Color.LimeGreen);

        public ToggleNode FireTowerGround { get; set; } = new ToggleNode(true);
        public ToggleNode ColdTowerGround { get; set; } = new ToggleNode(true);
        public ToggleNode LightningTowerGround { get; set; } = new ToggleNode(true);
        public ToggleNode PhysicalTowerGround { get; set; } = new ToggleNode(true);
        public ToggleNode MinionTowerGround { get; set; } = new ToggleNode(true);
        public ToggleNode BuffTowerGround { get; set; } = new ToggleNode(true);
    }
}