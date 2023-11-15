using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Helpers;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace Blight
{
    public class Main : BaseSettingsPlugin<BlightSettings>
    {
        private Camera Camera => GameController.Game.IngameState.Camera;
        private double _mapScale;
        private Vector2 _mapCenter;
        private bool _largeMapOpen;
        private Vector2 _playerGridPos;
        private float _playerZ;

        private const double CameraAngle = 38.7 * Math.PI / 180;
        private static readonly float CameraAngleCos = (float)Math.Cos(CameraAngle);
        private static readonly float CameraAngleSin = (float)Math.Sin(CameraAngle);

        public List<Entity> BlightEntities = new List<Entity>();
        public List<Entity> drawList = new List<Entity>();

        private const float GridToWorldMultiplier = 250 / 23f;
        public override bool Initialise()
        {

            return true;
        }

        public override void AreaChange(AreaInstance area)
        {
            BlightEntities.Clear();
        }

        public override Job Tick()
        {
            drawList.Clear();

            if (Settings.Enable)
            {
                _playerGridPos = GameController.Player.GetComponent<Positioned>().WorldPosNum.WorldToGrid();

                var ingameUi = GameController.Game.IngameState.IngameUi;
                var map = ingameUi.Map;
                var largeMap = map.LargeMap.AsObject<SubMap>();
                _largeMapOpen = largeMap.IsVisible;
                _mapScale = GameController.IngameState.Camera.Height / 677f * largeMap.Zoom;
                _mapCenter = largeMap.GetClientRect().TopLeft.ToVector2Num() + largeMap.ShiftNum + largeMap.DefaultShiftNum;
                _playerZ = GameController.Player.GetComponent<Render>().Z;

                var sortedList = BlightEntities.OrderByDescending(item => item.Id).ToList();

                drawList = sortedList;
            };

            return null;
        }

        public override void Render()
        {
            DrawLines(drawList, Settings.BlightWidthMap, Settings.BlightColorMap, Settings.BlightWidthWorld, Settings.BlightColorWorld);
        }

        public override void EntityAdded(Entity entity)
        {
            if (entity.Metadata == "Metadata/Terrain/Leagues/Blight/Objects/BlightPathway")
            {
                BlightEntities.Add(entity);
            }
        }

        private Vector3 ExpandWithTerrainHeight(Vector2 gridPosition)
        {
            return new Vector3(gridPosition.GridToWorld(), GameController.IngameState.Data.GetTerrainHeightAt(gridPosition));
        }
        private Vector2 GetWorldScreenPosition(Vector2 gridPos)
        {
            return Camera.WorldToScreen(ExpandWithTerrainHeight(gridPos));
        }
        private Vector2 GetMapScreenPosition(Vector2 gridPos)
        {
            return _mapCenter + TranslateGridDeltaToMapDelta(gridPos - _playerGridPos, GameController.IngameState.Data.GetTerrainHeightAt(gridPos) - _playerZ);
        }
        private Vector2 TranslateGridDeltaToMapDelta(Vector2 delta, float deltaZ)
        {
            deltaZ /= GridToWorldMultiplier; //z is normally "world" units, translate to grid
            return (float)_mapScale * new Vector2((delta.X - delta.Y) * CameraAngleCos, (deltaZ - (delta.X + delta.Y)) * CameraAngleSin);
        }

        public void DrawLines(List<Entity> drawingOrder, float lineWidthMap, Color lineColorMap, float lineWidthWorld, Color lineColorWorld)
        {
            if (drawingOrder == null || drawingOrder.Count < 2) return;

            for (int i = 0; i < drawingOrder.Count - 1; i++)
            {
                var entity1 = drawingOrder[i];
                var entity2 = drawingOrder[i + 1];

                if (entity1.Id - entity2.Id > 1 || entity1.Distance(entity2) > 35)
                {
                        continue;
                }

                if (Settings.DrawMap && _largeMapOpen)
                {
                    Graphics.DrawLine(GetMapScreenPosition(entity1.GridPosNum), GetMapScreenPosition(entity2.GridPosNum), lineWidthMap, lineColorMap);
                }
                if (Settings.DrawWorld && _playerGridPos.Distance(entity1.GridPosNum) < Settings.MaxWorldDrawDistance && _playerGridPos.Distance(entity2.GridPosNum) < Settings.MaxWorldDrawDistance)
                {
                    Graphics.DrawLine(GetWorldScreenPosition(entity1.GridPosNum), GetWorldScreenPosition(entity2.GridPosNum), lineWidthWorld, lineColorWorld);
                    //Graphics.DrawText(entity1.Id.ToString(), GetWorldScreenPosition(entity1.GridPosNum));
                }
            }
        }
    }
}