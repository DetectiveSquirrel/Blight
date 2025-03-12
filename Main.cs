using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Helpers;
using SharpDX;
using Vector3 = System.Numerics.Vector3;

namespace Blight;

public class Main : BaseSettingsPlugin<BlightSettings>
{
    public Entity AreaPumpEntity { get; private set; }
    public bool AreaPumpStopped { get; private set; }
    public List<Entity> BlightPathways { get; private set; } = new();
    public bool LargeMapOpen { get; private set; }

    public List<Entity> PathwayEntityList { get; } = new();

    public List<Entity> TowerEntityList { get; } = new();

    public override bool Initialise()
    {
        PathwayEntityList.Clear();
        TowerEntityList.Clear();
        AreaPumpEntity = null;
        AreaPumpStopped = false;
        return true;
    }

    public override void AreaChange(AreaInstance area)
    {
        PathwayEntityList.Clear();
        TowerEntityList.Clear();
        AreaPumpEntity = null;
        AreaPumpStopped = false;
    }

    public override Job Tick()
    {
        if (!Settings.Enable || !GameController.InGame || AreaPumpStopped)
            return null;

        LargeMapOpen = GameController.Game.IngameState.IngameUi.Map.LargeMap.IsVisible;
        BlightPathways = PathwayEntityList.OrderByDescending(item => item.Id).ToList();

        if (AreaPumpEntity != null &&
            AreaPumpEntity.TryGetComponent<StateMachine>(out var stateComponent) &&
            stateComponent.States.Any(state => state.Name is "success" or "fail" && state.Value == 1))
            AreaPumpStopped = true;

        return null;
    }

    public override void Render()
    {
        if (!Settings.Enable || AreaPumpStopped || !GameController.InGame || GameController.IngameState.Data == null)
            return;

        var inGameUi = GameController.Game.IngameState.IngameUi;

        if (Settings.DisableDrawOnLeftOrRightPanelsOpen && (inGameUi.OpenLeftPanel.IsVisible || inGameUi.OpenRightPanel.IsVisible))
            return;

        if (!Settings.IgnoreFullscreenPanels && inGameUi.FullscreenPanels.Any(x => x.IsVisible))
            return;

        if (!Settings.IgnoreLargePanels && inGameUi.LargePanels.Any(x => x.IsVisible))
            return;

        DrawBlightPaths(BlightPathways);
        DrawTowers(TowerEntityList);
    }

    private void DrawTowers(List<Entity> towerList)
    {
        foreach (var tower in towerList)
        {
            if (!tower.TryGetComponent<BlightTower>(out var blightTowerComp))
                continue;

            var radius = GetTowerRadius(blightTowerComp.Id);
            if (radius == -1)
                continue;

            DrawTower(tower, blightTowerComp.Id, radius);
        }
    }

    private void DrawTower(Entity tower, string towerId, int radius)
    {
        var worldRadius = radius / PoeMapExtension.WorldToGridConversion;
        var isWithinScreen = IsEntityWithinScreen(tower.PosNum, GetScreenSize(), 200);

        var towerSetting = towerId switch
        {
            "FlameTower1" or "FlameTower2" or "FlameTower3" or "MeteorTower" or "FlamethrowerTower" => Settings.Towers.FireTower,

            "ChillingTower1" or "ChillingTower2" or "ChillingTower3" or "FreezingTower" or "IcePrisonTower" => Settings.Towers.ColdTower,

            "ShockingTower1" or "ShockingTower2" or "ShockingTower3" or "LightningStormTower" or "ArcingTower" => Settings.Towers.LightningTower,

            "StunningTower1" or "StunningTower2" or "StunningTower3" or "TemporalTower" or "PetrificationTower" => Settings.Towers.PhysicalTower,

            "MinionTower1" or "MinionTower2" or "MinionTower3" or "FlyingMinionTower" or "TankyMinionTower" => Settings.Towers.MinionTower,

            "BuffTower1" or "BuffTower2" or "BuffTower3" or "BuffPlayersTower" or "WeakenEnemiesTower" => Settings.Towers.BuffTower,

            _ => null
        };

        if (towerSetting == null)
            return;

        if (towerSetting.DrawGround && isWithinScreen)
            Graphics.DrawCircleInWorld(tower.PosNum, worldRadius, towerSetting.Color, towerSetting.CircleThickness, 40, Settings.Towers.FollowWorldTerrain.Value);

        if (towerSetting.DrawMap && LargeMapOpen)
            Graphics.DrawCircleOnLargeMap(tower.GridPosNum, Settings.Towers.FollowWorldTerrain.Value, radius, towerSetting.Color, towerSetting.CircleThickness / 4, 40);
    }

    public override void EntityAdded(Entity entity)
    {
        if (entity.Metadata == "Metadata/Terrain/Leagues/Blight/Objects/BlightPathway")
            PathwayEntityList.Add(entity);

        if (entity.TryGetComponent<BlightTower>(out var blightComp))
            TowerEntityList.Add(entity);

        if (entity.Metadata == "Metadata/Terrain/Leagues/Blight/Objects/BlightPump")
            AreaPumpEntity = entity;
    }

    public override void EntityRemoved(Entity entity)
    {
        var entityToRemove = TowerEntityList.FirstOrDefault(tower => tower.Id == entity.Id);

        if (entityToRemove != null)
            TowerEntityList.Remove(entityToRemove);
    }

    public void DrawBlightPaths(List<Entity> drawingOrder)
    {
        if (drawingOrder == null || drawingOrder.Count < 2)
            return;

        for (var i = 0; i < drawingOrder.Count - 1; i++)
        {
            var entity1 = drawingOrder[i];
            var entity2 = drawingOrder[i + 1];

            if (entity1.Id - entity2.Id > 1 || entity1.Distance(entity2) > 35)
                continue;

            if (Settings.Pathways.DrawMap && LargeMapOpen)
                Graphics.DrawLineOnLargeMap(entity1.GridPosNum, entity2.GridPosNum, Settings.Pathways.MapLineWidth, Settings.Pathways.MapColor);

            if (Settings.Pathways.DrawWorld && IsEntityWithinScreen(entity1.PosNum, GetScreenSize(), 200))
                Graphics.DrawLineInWorld(entity1.GridPosNum, entity2.GridPosNum, Settings.Pathways.WorldLineWidth, Settings.Pathways.WorldColor);
        }
    }

    public RectangleF GetScreenSize()
    {
        var size = GameController.Window.GetWindowRectangleTimeCache.Size;
        return new RectangleF { X = 0, Y = 0, Width = size.Width, Height = size.Height };
    }

    public int GetTowerRadius(string blightTowerId)
    {
        var record = GameController.Game.Files.BlightTowers.EntriesList.FirstOrDefault(r => r.Id == blightTowerId);
        return record?.Radius ?? -1;
    }

    private static bool IsEntityWithinScreen(Vector3 entityPos, RectangleF screenSize, float allowancePx)
    {
        var entityScreenPos = RemoteMemoryObject.pTheGame.IngameState.Camera.WorldToScreen(entityPos);

        var leftBound = screenSize.Left - allowancePx;
        var rightBound = screenSize.Right + allowancePx;
        var topBound = screenSize.Top - allowancePx;
        var bottomBound = screenSize.Bottom + allowancePx;

        return entityScreenPos.X >= leftBound && entityScreenPos.X <= rightBound && entityScreenPos.Y >= topBound && entityScreenPos.Y <= bottomBound;
    }
}