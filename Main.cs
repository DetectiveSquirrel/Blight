using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Helpers;
using SharpDX;
using System.Collections.Generic;
using System.Linq;
using Vector3 = System.Numerics.Vector3;

namespace Blight;

public class Main : BaseSettingsPlugin<BlightSettings>
{
    private readonly List<BlightTowerEntry> _blightTowerEntries = new();
    private readonly Dictionary<uint, bool> _pathwayClosed = new();
    private readonly Dictionary<string, int> _towerRadiusById = new();

    public Entity AreaPumpEntity { get; private set; }
    public bool AreaPumpStopped { get; private set; }
    public List<Entity> BlightPathways => PathwayEntityList;
    public bool LargeMapOpen { get; private set; }

    public List<Entity> PathwayEntityList { get; } = new();

    public List<Entity> TowerEntityList { get; } = new();

    public List<ServerDataMinimapIcon> MiniMapIconList { get; set; } = [];

    public override bool Initialise()
    {
        ResetAreaState();
        return true;
    }

    public override void AreaChange(AreaInstance area) => ResetAreaState();

    private void ResetAreaState()
    {
        PathwayEntityList.Clear();
        TowerEntityList.Clear();
        _blightTowerEntries.Clear();
        _pathwayClosed.Clear();
        _towerRadiusById.Clear();
        AreaPumpEntity = null;
        AreaPumpStopped = false;
    }

    public override Job Tick()
    {
        if (!Settings.Enable || !GameController.InGame || AreaPumpStopped) return null;

        LargeMapOpen = GameController.Game.IngameState.IngameUi.Map.LargeMap.IsVisible;

        if (AreaPumpEntity != null &&
            AreaPumpEntity.TryGetComponent<StateMachine>(out var stateComponent) &&
            stateComponent.States.Any(state => state.Name is "success" or "fail" && state.Value == 1)) AreaPumpStopped = true;

        MiniMapIconList = GameController.Game.IngameState.ServerData.MinimapIcons;

        return null;
    }

    public override void Render()
    {
        if (!Settings.Enable || !GameController.InGame || GameController.IngameState.Data == null || AreaPumpStopped) return;

        if (!HasAnythingToDrawThisFrame()) return;

        var inGameUi = GameController.Game.IngameState.IngameUi;

        if (Settings.DisableDrawOnLeftOrRightPanelsOpen && (inGameUi.OpenLeftPanel.IsVisible || inGameUi.OpenRightPanel.IsVisible)) return;

        if (!Settings.IgnoreFullscreenPanels && inGameUi.FullscreenPanels.Any(x => x.IsVisible)) return;

        if (!Settings.IgnoreLargePanels && inGameUi.LargePanels.Any(x => x.IsVisible)) return;

        var screen = ScreenRect();
        DrawBlightPaths(PathwayEntityList, screen);
        DrawTowers(screen);
    }

    private bool HasAnythingToDrawThisFrame()
    {
        if (_blightTowerEntries.Count > 0) return true;

        var paths = Settings.Pathways;
        if (PathwayEntityList.Count < 2) return false;

        return paths.DrawWorld || paths.DrawMap && LargeMapOpen;
    }

    private void DrawTowers(RectangleF screen)
    {
        if (_blightTowerEntries.Count == 0) return;

        var followTerrain = Settings.Towers.FollowWorldTerrain.Value;

        foreach (var entry in _blightTowerEntries)
        {
            var radius = entry.Radius;
            if (radius < 0)
            {
                radius = GetTowerRadiusCached(entry.TowerId);
                if (radius < 0) continue;
                entry.Radius = radius;
            }

            DrawTower(entry, radius, screen, followTerrain);
        }
    }

    private void DrawTower(BlightTowerEntry entry, int radius, RectangleF screen, bool followTerrain)
    {
        var tower = entry.Entity;
        var worldRadius = radius / PoeMapExtension.WorldToGridConversion;
        var isWithinScreen = IsEntityWithinScreen(tower.PosNum, screen, 200);

        var draw = entry.Settings.DrawStyle;

        if (draw.Ground.Draw && isWithinScreen)
        {
            switch (draw.Ground.Style)
            {
                case CircleDrawStyle.Filled:
                    Graphics.DrawFilledCircleInWorld(tower.PosNum, worldRadius, draw.Ground.Color, 40, followTerrain);
                    break;
                case CircleDrawStyle.Outline:
                    Graphics.DrawCircleInWorld(tower.PosNum, worldRadius, draw.Ground.Color, draw.CircleThickness, 40, followTerrain);
                    break;
            }
        }

        if (draw.Map.Draw && LargeMapOpen)
        {
            switch (draw.Map.Style)
            {
                case CircleDrawStyle.Filled:
                    Graphics.DrawFilledCircleOnLargeMap(tower.GridPosNum, followTerrain, radius, draw.Map.Color, 40);
                    break;
                case CircleDrawStyle.Outline:
                    Graphics.DrawCircleOnLargeMap(tower.GridPosNum, followTerrain, radius, draw.Map.Color, draw.CircleThickness / 4, 40);
                    break;
            }
        }
    }

    public override void EntityAdded(Entity entity)
    {
        if (entity.Metadata == "Metadata/Terrain/Leagues/Blight/Objects/BlightPathway")
        {
            InsertPathwaySortedByIdDesc(PathwayEntityList, entity);
            PathwayClosed(entity);
        }

        if (entity.TryGetComponent<BlightTower>(out var blightComp))
        {
            var settings = ResolveTowerSettings(blightComp.Id);
            if (settings != null)
            {
                TowerEntityList.Add(entity);
                _blightTowerEntries.Add(new BlightTowerEntry(entity, blightComp.Id, settings, GetTowerRadiusCached(blightComp.Id)));
            }
        }

        if (entity.Metadata == "Metadata/Terrain/Leagues/Blight/Objects/BlightPump") AreaPumpEntity = entity;
    }

    public override void EntityRemoved(Entity entity)
    {
        if (entity.Metadata == "Metadata/Terrain/Leagues/Blight/Objects/BlightPathway")
        {
            PathwayEntityList.RemoveAll(p => p.Id == entity.Id);
            _pathwayClosed.Remove(entity.Id);
        }

        TowerEntityList.RemoveAll(t => t.Id == entity.Id);
        _blightTowerEntries.RemoveAll(t => t.Entity.Id == entity.Id);
    }

    private TowerSettings? ResolveTowerSettings(string towerId) => towerId switch
    {
        "FlameTower1" or "FlameTower2" or "FlameTower3" or "MeteorTower" or "FlamethrowerTower" => Settings.Towers.FireTower,
        "ChillingTower1" or "ChillingTower2" or "ChillingTower3" or "FreezingTower" or "IcePrisonTower" => Settings.Towers.ColdTower,
        "ShockingTower1" or "ShockingTower2" or "ShockingTower3" or "LightningStormTower" or "ArcingTower" => Settings.Towers.LightningTower,
        "StunningTower1" or "StunningTower2" or "StunningTower3" or "TemporalTower" or "PetrificationTower" => Settings.Towers.PhysicalTower,
        "MinionTower1" or "MinionTower2" or "MinionTower3" or "FlyingMinionTower" or "TankyMinionTower" => Settings.Towers.MinionTower,
        "BuffTower1" or "BuffTower2" or "BuffTower3" or "BuffPlayersTower" or "WeakenEnemiesTower" => Settings.Towers.BuffTower,
        _ => null
    };

    private static void InsertPathwaySortedByIdDesc(List<Entity> list, Entity entity)
    {
        var id = entity.Id;
        var i = 0;
        for (; i < list.Count; i++)
        {
            if (list[i].Id < id) break;
        }

        list.Insert(i, entity);
    }

    private bool PathwayClosed(Entity e)
    {
        if (!e.TryGetComponent<StateMachine>(out var sm) || sm.States == null || sm.States.All(s => s.Name != "visual")) return _pathwayClosed.TryGetValue(e.Id, out var c) && c;
        {
            var closed = sm.States.Any(s => s.Name == "visual" && s.Value >= 3);
            _pathwayClosed[e.Id] = closed;
            return closed;
        }
    }

    private void DrawBlightPaths(List<Entity> drawingOrder, RectangleF screen)
    {
        if (drawingOrder == null || drawingOrder.Count < 2) return;

        var p = Settings.Pathways;
        var drawMap = p.DrawMap && LargeMapOpen;
        var drawWorld = p.DrawWorld;
        if (!drawMap && !drawWorld) return;

        var skipClosed = p.DisableWhenPathwayClosed.Value;

        for (var i = 0; i < drawingOrder.Count - 1; i++)
        {
            var entity1 = drawingOrder[i];
            var entity2 = drawingOrder[i + 1];

            if (skipClosed && PathwayClosed(entity1)) continue;

            if (entity1.Id - entity2.Id > 1 || entity1.Distance(entity2) > 35) continue;

            if (drawMap) Graphics.DrawLineOnLargeMap(entity1.GridPosNum, entity2.GridPosNum, p.MapLineWidth, p.MapColor);

            if (drawWorld && IsEntityWithinScreen(entity1.PosNum, screen, 200)) Graphics.DrawLineInWorld(entity1.GridPosNum, entity2.GridPosNum, p.WorldLineWidth, p.WorldColor);
        }
    }

    private RectangleF ScreenRect()
    {
        var size = GameController.Window.GetWindowRectangleTimeCache.Size;
        return new RectangleF {X = 0, Y = 0, Width = size.Width, Height = size.Height};
    }

    private int GetTowerRadiusCached(string blightTowerId)
    {
        if (_towerRadiusById.TryGetValue(blightTowerId, out var cached)) return cached;

        var record = GameController.Game.Files.BlightTowers.EntriesList.FirstOrDefault(r => r.Id == blightTowerId);
        cached = record?.Radius ?? -1;
        _towerRadiusById[blightTowerId] = cached;
        return cached;
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

    private sealed class BlightTowerEntry(Entity entity, string towerId, TowerSettings settings, int radius)
    {
        public int Radius = radius;
        public Entity Entity { get; } = entity;
        public string TowerId { get; } = towerId;
        public TowerSettings Settings { get; } = settings;
    }
}