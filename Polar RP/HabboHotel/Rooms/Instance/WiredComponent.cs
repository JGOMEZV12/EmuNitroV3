using Polar.HabboHotel.Rooms.Instance;
using Newtonsoft.Json;
using Polar.Core;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Items.Wired;
using Polar.HabboHotel.Items.Wired.Boxes;
using Polar.HabboHotel.Items.Wired.Boxes.Add_ons;
using Polar.HabboHotel.Items.Wired.Boxes.Conditions;
using Polar.HabboHotel.Items.Wired.Boxes.Effects;
using Polar.HabboHotel.Items.Wired.Boxes.Selectors;
using Polar.HabboHotel.Items.Wired.Boxes.Triggers;
using Polar.HabboHotel.Rooms.Wired;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;

namespace Polar.HabboHotel.Rooms.Instance;

public static class WiredBotSourceUtil
{
    public const int SOURCE_BOT_NAME = 100;

    public static int NormalizeBotSource(int value) =>
        NormalizeBotSource(value, SOURCE_BOT_NAME);

    public static int NormalizeBotSource(int value, int fallback) =>
        value switch
        {
            WiredSourceUtil.SOURCE_TRIGGER => value,
            SOURCE_BOT_NAME => value,
            WiredSourceUtil.SOURCE_SELECTOR => value,
            WiredSourceUtil.SOURCE_SIGNAL => value,
            _ => fallback
        };

    /// <summary>
    /// Resuelve los bots a afectar según el botSource.
    /// Sin WiredContext: usa directamente la sala y el nombre.
    /// </summary>
    public static List<RoomUser> ResolveBots(Room room, int botSource, string botName)
    {
        if (room == null) return new List<RoomUser>();

        if (botSource == SOURCE_BOT_NAME)
        {
            var bot = room.GetRoomUserManager().GetBotByName(botName);
            return bot != null
                ? new List<RoomUser> { bot }
                : new List<RoomUser>();
        }

        // SOURCE_TRIGGER / SOURCE_SELECTOR / SOURCE_SIGNAL:
        // devolver todos los bots de la sala
        return room.GetRoomUserManager()
                   .GetRoomUsers()
                   .Where(u => u != null && u.IsBot)
                   .ToList();
    }

    public static bool RequiresTriggeringUser(int botSource) =>
        botSource == WiredSourceUtil.SOURCE_TRIGGER;
}
#region WiredSourceUtil (partial port)

public static class WiredSourceUtil
{
    // Tipos de fuente — mirrors Java WiredSourceUtil constants
    public const int SOURCE_TRIGGER = 0;
    public const int SOURCE_CLICKED_USER = 11;
    public const int SOURCE_SELECTED = 100;
    public const int SOURCE_SECONDARY_SELECTED = 101;
    public const int SOURCE_SELECTOR = 200;
    public const int SOURCE_SIGNAL = 201;

    /// <summary>
    /// Resuelve la lista de ítems de furni según el tipo de fuente indicado.
    /// Versión simplificada para Polar (sin WiredContext completo).
    /// </summary>
    public static System.Collections.Generic.List<Item> ResolveItems(
        Room room,
        Item triggerItem,
        int sourceType,
        System.Collections.Generic.ICollection<Item> selectedItems)
    {
        if (room == null) return new System.Collections.Generic.List<Item>();

        switch (sourceType)
        {
            case SOURCE_SELECTED:
                return selectedItems != null
                    ? new System.Collections.Generic.List<Item>(selectedItems)
                    : new System.Collections.Generic.List<Item>();

            case SOURCE_TRIGGER:
            default:
                return triggerItem != null
                    ? new System.Collections.Generic.List<Item> { triggerItem }
                    : new System.Collections.Generic.List<Item>();
        }
    }

    /// <summary>
    /// Resuelve la lista de RoomUsers según el tipo de fuente indicado.
    /// </summary>
    public static System.Collections.Generic.List<RoomUser> ResolveUsers(
        Room room,
        RoomUser actor,
        int sourceType,
        System.Collections.Generic.ICollection<RoomUser> selectedUsers)
    {
        if (room == null) return new System.Collections.Generic.List<RoomUser>();

        switch (sourceType)
        {
            case SOURCE_SELECTED:
                return selectedUsers != null
                    ? new System.Collections.Generic.List<RoomUser>(selectedUsers)
                    : new System.Collections.Generic.List<RoomUser>();

            case SOURCE_TRIGGER:
            default:
                return actor != null
                    ? new System.Collections.Generic.List<RoomUser> { actor }
                    : new System.Collections.Generic.List<RoomUser>();
        }
    }

    public static bool IsDefaultUserSource(int value) =>
        value == SOURCE_TRIGGER ||
        value == SOURCE_CLICKED_USER ||
        value == SOURCE_SELECTOR ||
        value == SOURCE_SIGNAL;

    public static bool IsSelectableUserSource(int value) =>
        value == SOURCE_SELECTED || IsDefaultUserSource(value);
}

#endregion
public class WiredComponent
{
    private readonly Room _room;
    private RoomFurniVariableManager _furniVariableManager;
    private RoomUserVariableManager _userVariableManager;
    private RoomVariableManager _roomVariableManager;
    private readonly ConcurrentDictionary<int, IWiredItem> _wiredItems;

    // Índice secundario: (x,y) → lista de wired en esa celda
    // Elimina el scan O(n) en GetEffects/GetConditions/GetTriggers
    private readonly ConcurrentDictionary<long, List<IWiredItem>> _byCoord;

    // Cache de triggers por tipo → evita scan en TriggerEvent
    private readonly ConcurrentDictionary<WiredBoxType, List<IWiredItem>> _byType;

    // Key compacta para coordenada: evita allocar un objeto Point o string
    private static long CoordKey(int x, int y) => ((long)x << 32) | (uint)y;

    public WiredComponent(Room instance)
    {
        _room = instance;
        _wiredItems = new();
        _byCoord = new();
        _byType = new();
        _furniVariableManager = new RoomFurniVariableManager(_room);
        _userVariableManager = new RoomUserVariableManager(_room);
        _roomVariableManager = new RoomVariableManager(_room);
    }

    // ── OnCycle ────────────────────────────────────────────────────────────
    // Sin ToList() — iteramos Values directamente (snapshot-safe en .NET)
    public void OnCycle()
    {
        foreach (var kvp in _wiredItems)
        {
            if (_room.GetRoomItemHandler().GetItem(kvp.Key) == null)
            {
                TryRemove(kvp.Key);
                continue;
            }

            if (kvp.Value is IWiredCycle cycle)
            {
                if (cycle.TickCount <= 0)
                {
                    try
                    {
                        cycle.OnCycle();
                    }
                    catch (Exception ex)
                    {
                        Logging.LogWiredException($"[WIRED] OnCycle crash itemId={kvp.Key}: {ex.Message}");
                        // Forzar un delay para no ejecutar en cada tick si crashea
                        cycle.TickCount = 10;
                    }
                }
                else
                    cycle.TickCount--;
            }
        }
    }

    public IWiredItem GenerateNewBox(Item item) => item.GetBaseItem().WiredType
        switch
    {
        WiredBoxType.TriggerRoomEnter => new RoomEnterBox(_room, item),
        WiredBoxType.TriggerLeaveRoom => new UserLeavesRoomBox(_room, item),
        WiredBoxType.TriggerRepeat => new RepeaterBox(_room, item),
        WiredBoxType.TriggerStateChanges => new StateChangesBox(_room, item),
        WiredBoxType.TriggerUserSays => new UserSaysBox(_room, item),
        WiredBoxType.TriggerWalkOffFurni => new UserWalksOffBox(_room, item),
        WiredBoxType.TriggerWalkOnFurni => new UserWalksOnBox(_room, item),
        WiredBoxType.TriggerGameStarts => new GameStartsBox(_room, item),
        WiredBoxType.TriggerGameEnds => new GameEndsBox(_room, item),
        WiredBoxType.TriggerUserFurniCollision => new UserFurniCollision(_room, item),
        WiredBoxType.TriggerUserSaysCommand => new UserSaysCommandBox(_room, item),
        WiredBoxType.EffectShowMessage => new ShowMessageBox(_room, item),
        WiredBoxType.EffectTeleportToFurni => new TeleportUserBox(_room, item),
        WiredBoxType.EffectToggleFurniState => new ToggleFurniBox(_room, item),
        WiredBoxType.EffectMoveAndRotate => new MoveAndRotateBox(_room, item),
        WiredBoxType.EffectKickUser => new KickUserBox(_room, item),
        WiredBoxType.EffectMuteTriggerer => new MuteTriggererBox(_room, item),
        WiredBoxType.EffectGiveReward => new GiveRewardBox(_room, item),
        WiredBoxType.EffectMatchPosition => new MatchPositionBox(_room, item),
        WiredBoxType.EffectAddActorToTeam => new AddActorToTeamBox(_room, item),
        WiredBoxType.EffectCollisionCase => new CollisionCaseBox(_room, item),
        WiredBoxType.EffectRemoveActorFromTeam => new RemoveActorFromTeamBox(_room, item),
        WiredBoxType.EffectAddScore => new AddScoreBox(_room, item),
        WiredBoxType.ConditionFurniHasUsers => new FurniHasUsersBox(_room, item),
        WiredBoxType.ConditionTriggererOnFurni => new TriggererOnFurniBox(_room, item),
        WiredBoxType.ConditionTriggererNotOnFurni => new TriggererNotOnFurniBox(_room, item),
        WiredBoxType.ConditionFurniHasNoUsers => new FurniHasNoUsersBox(_room, item),
        WiredBoxType.ConditionFurniHasFurni => new FurniHasFurniBox(_room, item),
        WiredBoxType.ConditionIsGroupMember => new IsGroupMemberBox(_room, item),
        WiredBoxType.ConditionIsNotGroupMember => new IsNotGroupMemberBox(_room, item),
        WiredBoxType.ConditionUserCountInRoom => new UserCountInRoomBox(_room, item),
        WiredBoxType.ConditionUserCountDoesntInRoom => new UserCountDoesntInRoomBox(_room, item),
        WiredBoxType.ConditionIsWearingFX => new IsWearingFXBox(_room, item),
        WiredBoxType.ConditionIsNotWearingFX => new IsNotWearingFXBox(_room, item),
        WiredBoxType.ConditionIsWearingBadge => new IsWearingBadgeBox(_room, item),
        WiredBoxType.ConditionIsNotWearingBadge => new IsNotWearingBadgeBox(_room, item),
        WiredBoxType.ConditionMatchStateAndPosition => new FurniMatchStateAndPositionBox(_room, item),
        WiredBoxType.ConditionDontMatchStateAndPosition => new FurniDoesntMatchStateAndPositionBox(_room, item),
        WiredBoxType.ConditionActorHasHandItemBox => new ActorHasHandItemBox(_room, item),
        WiredBoxType.ConditionActorIsInTeamBox => new ActorIsInTeamBox(_room, item),
        WiredBoxType.AddonRandomEffect => new AddonRandomEffectBox(_room, item),
        WiredBoxType.EffectMoveFurniToNearestUser => new MoveFurniToUserBox(_room, item),
        WiredBoxType.EffectExecuteWiredStacks => new ExecuteWiredStacksBox(_room, item),
        WiredBoxType.EffectNegativeExecuteWiredStacks => new NegativeExecuteWiredStacksBox(_room, item),
        WiredBoxType.EffectTeleportBotToFurniBox => new TeleportBotToFurniBox(_room, item),
        WiredBoxType.EffectBotChangesClothesBox => new BotChangesClothesBox(_room, item),
        WiredBoxType.EffectBotMovesToFurniBox => new BotMovesToFurniBox(_room, item),
        WiredBoxType.EffectBotCommunicatesToAllBox => new BotCommunicatesToAllBox(_room, item),
        WiredBoxType.EffectBotGivesHanditemBox => new BotGivesHandItemBox(_room, item),
        WiredBoxType.EffectBotFollowsUserBox => new BotFollowsUserBox(_room, item),
        WiredBoxType.EffectSetRollerSpeed => new SetRollerSpeedBox(_room, item),
        WiredBoxType.EffectRegenerateMaps => new RegenerateMapsBox(_room, item),
        WiredBoxType.EffectGiveUserBadge => new GiveUserBadgeBox(_room, item),
        WiredBoxType.EffectGiveCurrency => new GiveCurrencyBox(_room, item),
        WiredBoxType.ConditionHasJob => new HasJobBox(_room, item),
        WiredBoxType.ConditionIsNight => new IsNightBox(_room, item),
        WiredBoxType.ConditionIsDay => new IsDayBox(_room, item),
        WiredBoxType.EffectExecuteCommand => new ExecuteCommandBox(_room, item),
        WiredBoxType.EffectGiveExperience => new GiveExperienceBox(_room, item),
        WiredBoxType.ConditionIsJailed => new IsJailedBox(_room, item),
        WiredBoxType.ConditionIsDead => new IsDeadBox(_room, item),
        WiredBoxType.EffectApplyEffect => new ApplyEffectBox(_room, item),
        WiredBoxType.ConditionIsDriving => new IsDrivingBox(_room, item),
        WiredBoxType.EffectDamageUser => new DamageUserBox(_room, item),
        WiredBoxType.EffectHealUser => new HealUserBox(_room, item),
        WiredBoxType.ConditionHasVip => new HasVipBox(_room, item),
        WiredBoxType.EffectSetMotto => new SetMottoBox(_room, item),
        WiredBoxType.EffectFreezeUser => new FreezeUserBox(_room, item),
        WiredBoxType.EffectUnfreezeUser => new UnfreezeUserBox(_room, item),
        WiredBoxType.ConditionIsSitting => new IsSittingBox(_room, item),
        WiredBoxType.EffectGiveHuntPoints => new GiveHuntPointsBox(_room, item),
        WiredBoxType.EffectGiveEnergy => new GiveEnergyBox(_room, item),
        WiredBoxType.EffectGiveArmor => new GiveArmorBox(_room, item),
        WiredBoxType.ConditionHasWeapon => new HasWeaponBox(_room, item),
        WiredBoxType.EffectGiveRPItem => new GiveRPItemBox(_room, item),
        WiredBoxType.ConditionHasRPItem => new HasRPItemBox(_room, item),
        WiredBoxType.EffectSetRotation => new SetRotationBox(_room, item),
        WiredBoxType.ConditionIsIdle => new IsIdleBox(_room, item),
        WiredBoxType.ConditionIsDancing => new IsDancingBox(_room, item),
        WiredBoxType.AddonAnimationTime => new AddonAnimationTimeBox(_room, item),
        WiredBoxType.AddonExecuteInOrder => new AddonExecuteInOrderBox(_room, item),
        WiredBoxType.AddonExecutionLimit => new AddonExecutionLimitBox(_room, item),
        WiredBoxType.AddonFilterFurni => new AddonFilterFurniBox(_room, item),
        WiredBoxType.AddonFilterUser => new AddonFilterUserBox(_room, item),
        WiredBoxType.AddonMoveCarryUsers => new AddonMoveCarryUsersBox(_room, item),
        WiredBoxType.AddonMoveNoAnimation => new AddonMoveNoAnimationBox(_room, item),
        WiredBoxType.AddonMovePhysics => new AddonMovePhysicsBox(_room, item),
        WiredBoxType.AddonOrEval => new AddonOrEvalBox(_room, item),
        WiredBoxType.AddonRandom => new AddonRandomBox(_room, item),
        WiredBoxType.AddonTextOutputFurniName => new AddonTextOutputFurniNameBox(_room, item),
        WiredBoxType.AddonTextOutputUsername => new AddonTextOutputUsernameBox(_room, item),
        WiredBoxType.AddonUnseen => new AddonUnseenBox(_room, item),
        WiredBoxType.EffectSetVariable => new SetVariableBox(_room, item),
        WiredBoxType.EffectVariableAdd => new VariableAddBox(_room, item),
        WiredBoxType.ConditionVariableIsEqual => new VariableIsEqualBox(_room, item),
        WiredBoxType.EffectVariableSubtract => new VariableSubtractBox(_room, item),
        WiredBoxType.ConditionVariableIsGreaterThan => new VariableIsGreaterThanBox(_room, item),
        WiredBoxType.ConditionVariableIsLessThan => new VariableIsLessThanBox(_room, item),
        WiredBoxType.EffectMoveFurniXYZ => new MoveFurniXYZBox(_room, item),
        WiredBoxType.EffectGiveHanditem => new GiveHanditemBox(_room, item),
        WiredBoxType.EffectTeleportToRoom => new TeleportToRoomBox(_room, item),
        WiredBoxType.ConditionNotActorHasHandItemBox => new NotActorHasHandItemBox(_room, item),
        WiredBoxType.ConditionFurniHasNoFurni => new FurniHasNoFurniBox(_room, item),
        WiredBoxType.EffectResetTimers => new ResetTimersBox(_room, item),
        WiredBoxType.ConditionFurniTypeMatches => new FurniTypeMatchesBox(_room, item),
        WiredBoxType.ConditionFurniTypeDoesntMatch => new FurniTypeDoesntMatchBox(_room, item),
        WiredBoxType.AddonSetVariable => new AddonSetVariableBox(_room, item),
        WiredBoxType.AddonVariableLevelUpSystem => new AddonVariableLevelUpSystemBox(_room, item),
        WiredBoxType.AddonVariableReference => new AddonVariableReferenceBox(_room, item),
        WiredBoxType.SelectorFurniArea => new FurniAreaBox(_room, item),
        WiredBoxType.SelectorFurniNeighborhood => new FurniNeighborhoodBox(_room, item),
        WiredBoxType.SelectorFurniByType => new FurniByTypeBox(_room, item),
        WiredBoxType.SelectorFurniAltitude => new FurniAltitudeBox(_room, item),
        WiredBoxType.SelectorFurniOnFurni => new FurniOnFurniBox(_room, item),
        WiredBoxType.SelectorFurniPicks => new FurniPicksBox(_room, item),
        WiredBoxType.SelectorFurniSignal => new FurniSignalBox(_room, item),
        WiredBoxType.SelectorFurniWithVariable => new FurniWithVariableBox(_room, item),
        WiredBoxType.SelectorUsersArea => new UsersAreaBox(_room, item),
        WiredBoxType.SelectorUsersNeighborhood => new UsersNeighborhoodBox(_room, item),
        WiredBoxType.SelectorUsersSignal => new UsersSignalBox(_room, item),
        WiredBoxType.SelectorUsersByType => new UsersByTypeBox(_room, item),
        WiredBoxType.SelectorUsersTeam => new UsersTeamBox(_room, item),
        WiredBoxType.SelectorUsersByAction => new UsersByActionBox(_room, item),
        WiredBoxType.SelectorUsersByName => new UsersByNameBox(_room, item),
        WiredBoxType.SelectorUsersHandItem => new UsersHandItemBox(_room, item),
        WiredBoxType.SelectorUsersOnFurni => new UsersOnFurniBox(_room, item),
        WiredBoxType.SelectorUsersGroup => new UsersGroupBox(_room, item),
        WiredBoxType.SelectorUsersWithVariable => new UsersWithVariableBox(_room, item),
        _ => LogAndReturnNull(item)
    };

    private IWiredItem LogAndReturnNull(Item item)
    {
        Logging.LogWiredException($"[WIRED] Tipo no registrado: {item.GetBaseItem().WiredType} (itemId={item.Id})");
        return null;
    }

    public bool OtherBoxHasItem(IWiredItem box, int itemId)
    {
        if (box == null) return false;

        // FIX: eliminado null check innecesario — GetEffects siempre devuelve una lista no-null
        foreach (var item in GetEffects(box).Where(x => x.Item.Id != box.Item.Id))
        {
            if (item.Type != WiredBoxType.EffectMoveAndRotate &&
                item.Type != WiredBoxType.EffectMoveFurniFromNearestUser &&
                item.Type != WiredBoxType.EffectMoveFurniToNearestUser)
                continue;
            if (item.SetItems?.ContainsKey(itemId) == true)
                return true;
        }
        return false;
    }

    public bool TriggerEvent(WiredBoxType type, params object[] @params)
    {
        try
        {
            if (!_byType.TryGetValue(type, out var candidates))
                return false;

            List<IWiredItem> snapshot;
            lock (candidates) { snapshot = candidates.ToList(); }

            if (type == WiredBoxType.TriggerUserSays)
            {
                string message = Convert.ToString(@params[1]);
                bool finished = false;
                foreach (var box in snapshot)
                {
                    if (message.Contains($" {box.StringData}") ||
                        message.Contains($"{box.StringData} ") ||
                        message == box.StringData)
                        finished = box.Execute(@params);
                }
                return finished;
            }

            bool result = false;
            foreach (var box in snapshot)
            {
                if (box != null)
                    result = box.Execute(@params);
            }
            return result;
        }
        catch { return false; }
    }

    public ICollection<IWiredItem> GetEffects(IWiredItem item)
    {
        var key = CoordKey(item.Item.GetX, item.Item.GetY);
        if (!_byCoord.TryGetValue(key, out var list))
            return new List<IWiredItem>();

        lock (list)
            return list.Where(i => IsEffect(i.Item)).OrderBy(i => i.Item.GetZ).ToList();
    }

    public ICollection<IWiredItem> GetConditions(IWiredItem item)
    {
        var key = CoordKey(item.Item.GetX, item.Item.GetY);
        if (!_byCoord.TryGetValue(key, out var list))
            return new List<IWiredItem>();

        lock (list)
            return list.Where(i => IsCondition(i.Item)).ToList();
    }

    public ICollection<IWiredItem> GetTriggers(IWiredItem item)
    {
        var key = CoordKey(item.Item.GetX, item.Item.GetY);
        if (!_byCoord.TryGetValue(key, out var list))
            return new List<IWiredItem>();

        lock (list)
            return list.Where(i => IsTrigger(i.Item)).ToList();
    }

    // FIX: Random.Shared.Next() — O(1) vs el anterior OrderBy(Guid.NewGuid()) que era O(n log n)
    public IWiredItem GetRandomEffect(ICollection<IWiredItem> effects)
    {
        if (effects == null || effects.Count == 0) return null;
        var list = effects as IList<IWiredItem> ?? effects.ToList();
        return list[Random.Shared.Next(list.Count)];
    }

    public bool OnUserFurniCollision(Room room, Item item)
    {
        if (room == null || item == null) return false;

        foreach (var point in item.GetSides())
        {
            if (!room.GetGameMap().SquareHasUsers(point.X, point.Y)) continue;
            var users = room.GetGameMap().GetRoomUsers(point);
            if (users == null || users.Count == 0) continue;
            foreach (var user in users.ToList())
            {
                if (user != null)
                    item.UserFurniCollision(user);
            }
        }
        return true;
    }

    public void OnEvent(Item item)
    {
        if (item.ExtraData == "1") return;
        item.ExtraData = "1";
        item.UpdateState(false, true);
        item.RequestUpdate(2, true);
    }

    public void SaveBox(IWiredItem item)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var I in item.SetItems.Values)
        {
            if (item.Type == WiredBoxType.EffectMatchPosition ||
                item.Type == WiredBoxType.ConditionMatchStateAndPosition ||
                item.Type == WiredBoxType.ConditionDontMatchStateAndPosition)
                sb.Append($"{I.Id}:{I.GetX},{I.GetY},{I.GetZ},{I.Rotation},{I.ExtraData};");
            else
                sb.Append($"{I.Id};");
        }

        string items = sb.ToString();

        if (item.Type == WiredBoxType.EffectMatchPosition ||
            item.Type == WiredBoxType.ConditionMatchStateAndPosition ||
            item.Type == WiredBoxType.ConditionDontMatchStateAndPosition)
            item.ItemsData = items;

        string json;

        // Si el box maneja su propio formato JSON, lo delegamos
        if (item is IWiredCustomData customData)
        {
            json = customData.GetWiredData();
        }
        else
        {
            int delay = item is IWiredCycle c ? c.Delay : 0;
            var data = new Dictionary<string, object>
        {
            { "delay", delay },
            { "string", item.StringData ?? "" },
            { "bool", item.BoolData },
            { "items", items }
        };
            json = JsonConvert.SerializeObject(data);
        }

        item.Item.WiredData = json;

        using var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
        dbClient.SetQuery("UPDATE `items` SET `wired_data` = @json WHERE `id` = @id");
        dbClient.AddParameter("id", item.Item.Id);
        dbClient.AddParameter("json", json);
        dbClient.RunQuery();
    }
    public RoomFurniVariableManager GetFurniVariableManager() => _furniVariableManager;
    public RoomUserVariableManager GetUserVariableManager() => _userVariableManager;
    public RoomVariableManager GetRoomVariableManager() => _roomVariableManager;
    // ── LoadWiredBox — sin cambios de lógica, solo usa nuevo AddBox ────────
    public IWiredItem LoadWiredBox(Item item) { LoadWiredBoxes(new[] { item }); TryGet(item.Id, out var box); return box; }

    public void LoadWiredBoxes(IEnumerable<Item> items)
    {
        var wiredItems = items.Where(i => i.IsWired).ToList();
        if (wiredItems.Count == 0) return;

        foreach (var item in wiredItems)
        {
            var newBox = GenerateNewBox(item);
            if (newBox == null) continue;

            if (!string.IsNullOrEmpty(item.WiredData))
            {
                ApplyDataToBox(newBox, item.WiredData);
            }
            else
            {
                newBox.ItemsData = "";
                newBox.StringData = "";
                newBox.BoolData = false;
                SaveBox(newBox);
            }

            if (!AddBox(newBox))
            {
                TryRemove(newBox.Item.Id);
                AddBox(newBox);
            }
        }
    }

    private void ApplyDataToBox(IWiredItem newBox, string wiredData)
    {
        try
        {
            // Si el box maneja su propio formato, lo delegamos
            if (newBox is IWiredCustomData customData)
            {
                customData.LoadWiredData(wiredData);
                return;
            }

            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(wiredData);
            if (data == null) return;

            string rawString = data.ContainsKey("string") ? Convert.ToString(data["string"]) : "";
            newBox.StringData = string.IsNullOrEmpty(rawString)
                ? newBox.Type switch
                {
                    WiredBoxType.ConditionMatchStateAndPosition or
                    WiredBoxType.ConditionDontMatchStateAndPosition or
                    WiredBoxType.EffectMatchPosition => "0;0;0",
                    WiredBoxType.ConditionUserCountInRoom or
                    WiredBoxType.ConditionUserCountDoesntInRoom or
                    WiredBoxType.EffectMoveAndRotate => "0;0",
                    WiredBoxType.ConditionFurniHasNoFurni => "0",
                    _ => ""
                }
                : rawString;

            newBox.BoolData = data.ContainsKey("bool") && Convert.ToBoolean(data["bool"]);
            newBox.ItemsData = data.ContainsKey("items") ? Convert.ToString(data["items"]) : "";

            if (newBox is IWiredCycle cycle && data.ContainsKey("delay"))
                cycle.Delay = Convert.ToInt32(data["delay"]);

            if (!string.IsNullOrEmpty(newBox.ItemsData))
            {
                foreach (var str in newBox.ItemsData.Split(';'))
                {
                    if (string.IsNullOrEmpty(str)) continue;
                    var sId = str.Contains(':') ? str.Split(':')[0] : str;
                    if (int.TryParse(sId, out int id))
                    {
                        var selectedItem = _room.GetRoomItemHandler().GetItem(id);
                        if (selectedItem != null)
                            newBox.SetItems.TryAdd(selectedItem.Id, selectedItem);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logging.LogWiredException($"[WIRED] Error deserializing wired data for item {newBox.Item.Id}: {ex.Message}");
        }
    }


    // ── Índices: mantener sincronizados con _wiredItems ────────────────────
    public bool AddBox(IWiredItem item)
    {
        if (!_wiredItems.TryAdd(item.Item.Id, item))
            return false;

        // Índice por coordenada
        var key = CoordKey(item.Item.GetX, item.Item.GetY);
        _byCoord.AddOrUpdate(key,
            _ => new List<IWiredItem> { item },
            (_, list) => { lock (list) { list.Add(item); } return list; });

        // Índice por tipo
        _byType.AddOrUpdate(item.Type,
            _ => new List<IWiredItem> { item },
            (_, list) => { lock (list) { list.Add(item); } return list; });

        return true;
    }

    public void Cleanup()
    {
        _wiredItems.Clear();
        _byCoord.Clear();
        _byType.Clear();
    }
    // En WiredComponent:
    public IEnumerable<IWiredItem> GetAllItems() => _wiredItems.Values;
    public bool TryRemove(int itemId)
    {
        if (!_wiredItems.TryRemove(itemId, out var item))
            return false;

        // Limpiar índice por coordenada
        var key = CoordKey(item.Item.GetX, item.Item.GetY);
        if (_byCoord.TryGetValue(key, out var coordList))
            lock (coordList) { coordList.Remove(item); }

        // Limpiar índice por tipo
        if (_byType.TryGetValue(item.Type, out var typeList))
            lock (typeList) { typeList.Remove(item); }

        return true;
    }

    public bool TryRemoveByCoord(int itemId, int oldX, int oldY)
    {
        if (!_wiredItems.TryRemove(itemId, out var item))
            return false;

        // Usar las coordenadas viejas que nos pasan, no las del ítem (ya actualizadas)
        var key = CoordKey(oldX, oldY);
        if (_byCoord.TryGetValue(key, out var coordList))
            lock (coordList) { coordList.Remove(item); }

        if (_byType.TryGetValue(item.Type, out var typeList))
            lock (typeList) { typeList.Remove(item); }

        return true;
    }
    public bool TryGet(int id, out IWiredItem item) => _wiredItems.TryGetValue(id, out item);
    public bool IsTrigger(Item item) => item.GetBaseItem().InteractionType == InteractionType.WIRED_TRIGGER;
    public bool IsEffect(Item item) => item.GetBaseItem().InteractionType == InteractionType.WIRED_EFFECT;
    public bool IsCondition(Item item) => item.GetBaseItem().InteractionType == InteractionType.WIRED_CONDITION;

    public bool IsAddon(Item item) => item.GetBaseItem().InteractionType == InteractionType.WIRED_ADDON;


    #region Variable Triggers (mirrors WiredManager.trigger*VariableChanged)

    /// <summary>
    /// Dispara el evento WiredBoxType.TriggerUserVariableChanged en la sala.
    /// Llamado por RoomUserVariableManager después de cualquier cambio.
    /// </summary>
    public static void TriggerUserVariableChanged(
        Room room, int userId, int definitionItemId,
        bool created, bool deleted, VariableChangeKind changeKind)
    {
        if (room == null || definitionItemId <= 0) return;

        // Obtener el RoomUser a partir del userId para enviarlo como actor
        RoomUser roomUser = room.GetRoomUserManager().GetRoomUserByHabbo(userId);

        // Disparar el evento wired correspondiente
        room.GetWired().TriggerEvent(
            WiredBoxType.TriggerRoomVariableChanged,
            roomUser?.GetClient()?.GetHabbo(),
            null,  // no hay un ítem de furni involucrado
            new object[] { userId, definitionItemId, created, deleted, (int)changeKind });
    }

    /// <summary>
    /// Dispara el evento WiredBoxType.TriggerFurniVariableChanged en la sala.
    /// Llamado por RoomFurniVariableManager después de cualquier cambio.
    /// </summary>
    public static void TriggerFurniVariableChanged(
        Room room, int furniId, int definitionItemId,
        bool created, bool deleted, VariableChangeKind changeKind)
    {
        if (room == null || furniId <= 0 || definitionItemId <= 0) return;

        Item furni = room.GetRoomItemHandler().GetItem(furniId);

        room.GetWired().TriggerEvent(
            WiredBoxType.TriggerFurniVariableChanged,
            null,
            furni,
            new object[] { furniId, definitionItemId, created, deleted, (int)changeKind });
    }

    /// <summary>
    /// Dispara el evento WiredBoxType.TriggerRoomVariableChanged en la sala.
    /// Llamado por RoomVariableManager después de cualquier cambio.
    /// </summary>
    public static void TriggerRoomVariableChanged(
        Room room, int definitionItemId, VariableChangeKind changeKind)
    {
        if (room == null || definitionItemId <= 0) return;

        room.GetWired().TriggerEvent(
            WiredBoxType.TriggerRoomVariableChanged,
            null,
            null,
            new object[] { definitionItemId, (int)changeKind });
    }

    #endregion


    // ── 6. Diagnósticos (equivalentes a WiredManager.getDiagnosticsSnapshot / clear) ──

    #region Diagnostics (mirrors WiredManager.getDiagnosticsSnapshot / clearDiagnosticsLogs)

    // En Java, WiredManager delega en un WiredEngine interno.
    // En Polar, lo simplificamos con un log en memoria por sala.

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, System.Collections.Generic.List<string>>
        _diagnosticsLogs = new();

    public static void LogDiagnostic(int roomId, string message)
    {
        var list = _diagnosticsLogs.GetOrAdd(roomId,
            _ => new System.Collections.Generic.List<string>());
        lock (list)
        {
            list.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            if (list.Count > 200) list.RemoveAt(0); // cap en 200 entradas
        }
    }

    /// <summary>Returns a snapshot (copy) of the diagnostics log for a room.</summary>
    public static System.Collections.Generic.IReadOnlyList<string> GetDiagnosticsSnapshot(int roomId)
    {
        if (!_diagnosticsLogs.TryGetValue(roomId, out var list))
            return System.Array.Empty<string>();

        lock (list)
            return list.ToArray();
    }

    /// <summary>Clears the diagnostics log for a room.</summary>
    public static void ClearDiagnosticsLogs(int roomId)
    {
        if (_diagnosticsLogs.TryGetValue(roomId, out var list))
            lock (list)
                list.Clear();
    }

    #endregion


    // ── 7. Index helpers (mirrors WiredManager.invalidateRoom / invalidateTile / rebuildRoom) ──

    #region Index helpers

    /// <summary>
    /// Invalida el caché de stacks wired para toda la sala.
    /// Llamar cuando se añade, mueve o elimina un ítem wired.
    /// </summary>
    public void InvalidateRoom()
    {
        // Si tienes un índice interno de stacks (similar a RoomWiredStackIndex en Java),
        // llama aquí a su método de invalidación.
        // Ejemplo mínimo: limpiar el caché de wired boxes cargados.
        LoadWiredBoxes(_room.GetRoomItemHandler().GetFloor);
    }

    /// <summary>
    /// Invalida el caché de stacks wired para un tile concreto.
    /// </summary>
    public void InvalidateTile(int x, int y)
    {
        // Solo recarga los items wired de ese tile específico
        var items = _room.GetRoomItemHandler().GetFloor
            .Where(i => i.IsWired && i.GetX == x && i.GetY == y);
        LoadWiredBoxes(items);
    }

    /// <summary>
    /// Reconstruye el índice wired completo para la sala.
    /// </summary>
    public void RebuildRoom()
    {
        LoadWiredBoxes(_room.GetRoomItemHandler().GetFloor);
    }

    #endregion

}