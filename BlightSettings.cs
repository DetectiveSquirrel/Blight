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
    }
}