using ExileCore;
using ExileCore.PoEMemory;
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

namespace Blight;

public class Main : BaseSettingsPlugin<BlightSettings>
{
    private const double CameraAngle = 38.7 * Math.PI / 180;

    private const float GridToWorldMultiplier = 250 / 23f;
    private static readonly float CameraAngleCos = (float)Math.Cos(CameraAngle);
    private static readonly float CameraAngleSin = (float)Math.Sin(CameraAngle);

    private bool _largeMapOpen;
    private Vector2 _mapCenter;
    private double _mapScale;
    private Vector2 _playerGridPos;
    private float _playerZ;
    private Entity areaPumpEntity;
    private bool areaPumpStopped;

    public List<Entity> BlightEntities = [];
    public List<Entity> drawList = [];
    private List<Entity> towerList = [];

    public Vector3 PlayerPos { get; set; }
    public IngameData IngameData { get; set; }
    private Camera Camera => GameController.Game.IngameState.Camera;

    public override bool Initialise()
    {
        BlightEntities = [];
        towerList = [];
        areaPumpEntity = null;
        areaPumpStopped = false;
        return true;
    }

    public override void AreaChange(AreaInstance area)
    {
        BlightEntities = [];
        towerList = [];
        areaPumpEntity = null;
        areaPumpStopped = false;
    }

    public override Job Tick()
    {
        drawList.Clear();

        if (!Settings.Enable || !GameController.InGame || areaPumpStopped)
            return null;

        var Player = GameController?.Player;
        if (Player == null)
            return null;

        _playerGridPos = Player.GetComponent<Positioned>().WorldPosNum.WorldToGrid();
        var ingameUi = GameController.Game.IngameState.IngameUi;
        var map = ingameUi.Map;
        var largeMap = map.LargeMap.AsObject<SubMap>();
        _largeMapOpen = largeMap.IsVisible;
        _mapScale = GameController.IngameState.Camera.Height / 677f * largeMap.Zoom;
        _mapCenter = largeMap.GetClientRect().TopLeft.ToVector2Num() + largeMap.ShiftNum + largeMap.DefaultShiftNum;
        _playerZ = Player.GetComponent<Render>().Z;
        var sortedList = BlightEntities.OrderByDescending(item => item.Id).ToList();
        drawList = sortedList;
        IngameData = GameController.IngameState.Data;

        PlayerPos = IngameData.ToWorldWithTerrainHeight(Player.PosNum);

        // Check if pump running or not
        if (areaPumpEntity != null)
        {
            areaPumpEntity.TryGetComponent<StateMachine>(out var stateComponent);

            if (stateComponent != null)
            {
                foreach (var state in stateComponent.States)
                    if (state is { Name: "success" or "fail", Value: 1 })
                        areaPumpStopped = true;
            }
        }

        return null;
    }

    public override void Render()
    {
        if (!Settings.Enable || areaPumpStopped || !GameController.InGame || IngameData == null || PlayerPos == Vector3.Zero)
            return;

        var inGameUi = GameController.Game.IngameState.IngameUi;

        if (Settings.DisableDrawOnLeftOrRightPanelsOpen && (inGameUi.OpenLeftPanel.IsVisible || inGameUi.OpenRightPanel.IsVisible))
            return;

        if (!Settings.IgnoreFullscreenPanels && inGameUi.FullscreenPanels.Any(x => x.IsVisible))
            return;

        if (!Settings.IgnoreLargePanels && inGameUi.LargePanels.Any(x => x.IsVisible))
            return;

        DrawLines(drawList, Settings.BlightWidthMap, Settings.BlightColorMap, Settings.BlightWidthWorld, Settings.BlightColorWorld);
        DrawTowers();
    }

    private void DrawTowers()
    {
        foreach (var tower in towerList)
        {
            tower.TryGetComponent<BlightTower>(out var blightTowerComp);

            if (blightTowerComp == null)
                continue;

            var radius = GetTowerRadius(blightTowerComp.Id);

            if (radius == -1)
                continue;

            var worldRadius = radius / PoeMapExtension.WorldToGridConversion;
            var entityPos = tower.PosNum;
            var entityPosScreen = RemoteMemoryObject.pTheGame.IngameState.Camera.WorldToScreen(entityPos);
            var thickness = Settings.CircleThickness;

            if (IsEntityWithinScreen(entityPosScreen, GetScreenSize(), 200))
            {
                switch (blightTowerComp.Id)
                {
                    // Fire Towers
                    case "FlameTower1":
                    case "FlameTower2":
                    case "FlameTower3":
                    case "MeteorTower":
                    case "FlamethrowerTower":
                        if (Settings.FireTowerGround)
                            Graphics.DrawCircleInWorld(tower.PosNum, worldRadius, Settings.FireTowerColor, thickness, 40);

                        break;

                    // Cold Towers
                    case "ChillingTower1":
                    case "ChillingTower2":
                    case "ChillingTower3":
                    case "FreezingTower":
                    case "IcePrisonTower":
                        if (Settings.ColdTowerGround)
                            Graphics.DrawCircleInWorld(tower.PosNum, worldRadius, Settings.ColdTowerColor, thickness, 40);

                        break;

                    // Lightning Towers
                    case "ShockingTower1":
                    case "ShockingTower2":
                    case "ShockingTower3":
                    case "LightningStormTower":
                    case "ArcingTower":
                        if (Settings.LightningTowerGround)
                            Graphics.DrawCircleInWorld(tower.PosNum, worldRadius, Settings.LightningTowerColor, thickness, 40);

                        break;

                    // Physical Towers
                    case "StunningTower1":
                    case "StunningTower2":
                    case "StunningTower3":
                    case "TemporalTower":
                    case "PetrificationTower":
                        if (Settings.PhysicalTowerGround)
                            Graphics.DrawCircleInWorld(tower.PosNum, worldRadius, Settings.PhysicalTowerColor, thickness, 40);

                        break;

                    // Minion Towers
                    case "MinionTower1":
                    case "MinionTower2":
                    case "MinionTower3":
                    case "FlyingMinionTower":
                    case "TankyMinionTower":
                        if (Settings.MinionTowerGround)
                            Graphics.DrawCircleInWorld(tower.PosNum, worldRadius, Settings.MinionTowerColor, thickness, 40);

                        break;

                    // Buff Towers
                    case "BuffTower1":
                    case "BuffTower2":
                    case "BuffTower3":
                    case "BuffPlayersTower":
                    case "WeakenEnemiesTower":
                        if (Settings.BuffTowerGround)
                            Graphics.DrawCircleInWorld(tower.PosNum, worldRadius, Settings.BuffTowerColor, thickness, 40);

                        break;
                    default:
                        LogMessage($"Unconfirmed Tower: {blightTowerComp.Id} with radius: {radius}");
                        break;
                }
            }
        }
    }

    public override void EntityAdded(Entity entity)
    {
        if (entity.Metadata == "Metadata/Terrain/Leagues/Blight/Objects/BlightPathway")
            BlightEntities.Add(entity);

        entity.TryGetComponent<BlightTower>(out var blightComp);

        if (blightComp != null)
            towerList.Add(entity);

        if (entity.Metadata == "Metadata/Terrain/Leagues/Blight/Objects/BlightPump")
            areaPumpEntity = entity;
    }

    public override void EntityRemoved(Entity entity)
    {
        var entityToRemove = towerList.FirstOrDefault(tower => tower.Id == entity.Id);

        if (entityToRemove != null)
            towerList.Remove(entityToRemove);
    }

    private Vector3 ExpandWithTerrainHeight(Vector2 gridPosition) =>
        new(gridPosition.GridToWorld(), GameController.IngameState.Data.GetTerrainHeightAt(gridPosition));

    private Vector2 GetWorldScreenPosition(Vector2 gridPos) => Camera.WorldToScreen(ExpandWithTerrainHeight(gridPos));

    private Vector2 GetMapScreenPosition(Vector2 gridPos) =>
        _mapCenter + TranslateGridDeltaToMapDelta(gridPos - _playerGridPos, GameController.IngameState.Data.GetTerrainHeightAt(gridPos) - _playerZ);

    private Vector2 TranslateGridDeltaToMapDelta(Vector2 delta, float deltaZ)
    {
        deltaZ /= GridToWorldMultiplier; //z is normally "world" units, translate to grid
        return (float)_mapScale * new Vector2((delta.X - delta.Y) * CameraAngleCos, (deltaZ - (delta.X + delta.Y)) * CameraAngleSin);
    }

    public void DrawLines(List<Entity> drawingOrder, float lineWidthMap, Color lineColorMap, float lineWidthWorld, Color lineColorWorld)
    {
        if (drawingOrder == null || drawingOrder.Count < 2)
            return;

        for (var i = 0; i < drawingOrder.Count - 1; i++)
        {
            var entity1 = drawingOrder[i];
            var entity2 = drawingOrder[i + 1];

            if (entity1.Id - entity2.Id > 1 || entity1.Distance(entity2) > 35)
                continue;

            if (Settings.DrawMap && _largeMapOpen)
                Graphics.DrawLine(GetMapScreenPosition(entity1.GridPosNum), GetMapScreenPosition(entity2.GridPosNum), lineWidthMap, lineColorMap);

            if (Settings.DrawWorld && _playerGridPos.Distance(entity1.GridPosNum) < Settings.MaxWorldDrawDistance &&
                _playerGridPos.Distance(entity2.GridPosNum) < Settings.MaxWorldDrawDistance)
            {
                Graphics.DrawLine(
                    GetWorldScreenPosition(entity1.GridPosNum),
                    GetWorldScreenPosition(entity2.GridPosNum),
                    lineWidthWorld,
                    lineColorWorld
                );
                //Graphics.DrawText(entity1.Id.ToString(), GetWorldScreenPosition(entity1.GridPosNum));
            }
        }
    }

    public RectangleF GetScreenSize() =>
        new()
        {
            X = 0, Y = 0, Width = GameController.Window.GetWindowRectangleTimeCache.Size.Width,
            Height = GameController.Window.GetWindowRectangleTimeCache.Size.Height
        };

    public int GetTowerRadius(string blightTowerId)
    {
        var blightData = GameController.Game.Files.BlightTowers.EntriesList;

        foreach (var record in blightData.Where(record => record.Id == blightTowerId))
            return record.Radius;

        return -1;
    }

    private static bool IsEntityWithinScreen(Vector2 entityPos, RectangleF screenSize, float allowancePX)
    {
        // Check if the entity position is within the screen bounds with allowance
        var leftBound = screenSize.Left - allowancePX;
        var rightBound = screenSize.Right + allowancePX;
        var topBound = screenSize.Top - allowancePX;
        var bottomBound = screenSize.Bottom + allowancePX;
        return entityPos.X >= leftBound && entityPos.X <= rightBound && entityPos.Y >= topBound && entityPos.Y <= bottomBound;
    }
}