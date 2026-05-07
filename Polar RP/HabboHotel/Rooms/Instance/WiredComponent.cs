using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Items.Wired;
using Polar.HabboHotel.Items.Wired.Boxes;
using Polar.HabboHotel.Items.Wired.Boxes.Conditions;
using Polar.HabboHotel.Items.Wired.Boxes.Effects;
using Polar.HabboHotel.Items.Wired.Boxes.Triggers;
using Polar.HabboHotel.Items.Wired.Boxes.Add_ons;
using Polar.HabboHotel.Items.Wired.Boxes.Selectors;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Rooms.Instance;

public class WiredComponent
{
    private readonly Room _room;
    private readonly ConcurrentDictionary<int, IWiredItem> _wiredItems;
    private readonly ConcurrentDictionary<long, List<IWiredItem>> _byCoord;
    private readonly ConcurrentDictionary<WiredBoxType, List<IWiredItem>> _byType;

    private static long CoordKey(int x, int y) => ((long)x << 32) | (uint)y;

    public WiredComponent(Room instance)
    {
        _room = instance;
        _wiredItems = new();
        _byCoord = new();
        _byType = new();
    }

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
                    cycle.OnCycle();
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

        // Selectors
        WiredBoxType.EffectUsersOnFurni => new WiredEffectUsersOnFurni(_room, item),
        WiredBoxType.EffectUsersSignal => new WiredEffectUsersSignal(_room, item),
        WiredBoxType.EffectUsersGroup => new WiredEffectUsersGroup(_room, item),
        WiredBoxType.EffectUsersAction => new WiredEffectUsersAction(_room, item),
        WiredBoxType.EffectUsersPicks => new WiredEffectUsersOnFurni(_room, item), // placeholder
        WiredBoxType.EffectUsersAltitude => new WiredEffectUsersAltitude(_room, item),
        WiredBoxType.SelectorFurniOnFurni => new WiredEffectFurniOnFurni(_room, item),
        WiredBoxType.SelectorUsersArea => new WiredEffectUsersArea(_room, item),
        WiredBoxType.SelectorFurniArea => new WiredEffectFurniArea(_room, item),
        WiredBoxType.SelectorUsersTeam => new WiredEffectUsersTeam(_room, item),
        WiredBoxType.SelectorUsersHandItem => new WiredEffectUsersHandItem(_room, item),
        WiredBoxType.SelectorUsersNeighborhood => new WiredEffectUsersNeighborhood(_room, item),
        WiredBoxType.SelectorFurniNeighborhood => new WiredEffectFurniNeighborhood(_room, item),
        WiredBoxType.SelectorUsersByType => new WiredEffectUsersByType(_room, item),
        WiredBoxType.SelectorFurniByType => new WiredEffectFurniByType(_room, item),
        WiredBoxType.SelectorUsersByName => new WiredEffectUsersByName(_room, item),
        WiredBoxType.SelectorUsersWithVariable => new WiredEffectUsersWithVariable(_room, item),
        WiredBoxType.SelectorFurniWithVariable => new WiredEffectFurniWithVariable(_room, item),

        // Addons / Variables
        WiredBoxType.AddonSetVariable => new AddonSetVariableBox(_room, item),
        WiredBoxType.AddonUserVariable => new AddonUserVariableBox(_room, item),
        WiredBoxType.AddonRoomVariable => new AddonRoomVariableBox(_room, item),
        WiredBoxType.AddonFurniVariable => new AddonFurniVariableBox(_room, item),
        WiredBoxType.AddonContextVariable => new AddonContextVariableBox(_room, item),
        WiredBoxType.AddonVariableLevelUpSystem => new AddonVariableLevelUpSystemBox(_room, item),
        WiredBoxType.AddonVariableReference => new AddonVariableReferenceBox(_room, item),
        WiredBoxType.AddonTextInputVariable => new AddonTextInputVariableBox(_room, item),
        WiredBoxType.AddonTextOutputVariable => new AddonTextOutputVariableBox(_room, item),
        WiredBoxType.AddonVariableEcho => new AddonVariableEchoBox(_room, item),
        WiredBoxType.AddonFilterFurniByVariable => new AddonFilterFurniByVariableBox(_room, item),
        WiredBoxType.AddonFilterUsersByVariable => new AddonFilterUsersByVariableBox(_room, item),
        WiredBoxType.AddonVariableTextConnector => new AddonVariableTextConnectorBox(_room, item),

        _ => LogAndReturnNull(item)
    };

    private IWiredItem LogAndReturnNull(Item item)
    {
        Console.WriteLine($"[WIRED] Tipo no registrado: {item.GetBaseItem().WiredType} (itemId={item.Id})");
        return null;
    }

    public bool OtherBoxHasItem(IWiredItem box, int itemId)
    {
        if (box == null) return false;
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

    public ICollection<IWiredItem> GetTriggers(IWiredItem item) =>
        _wiredItems.Values
            .Where(i => IsTrigger(i.Item) && i.Item.GetX == item.Item.GetX && i.Item.GetY == item.Item.GetY)
            .ToList();

    public ICollection<IWiredItem> GetEffects(IWiredItem item) =>
        _wiredItems.Values
            .Where(i => (IsEffect(i.Item) || IsSelector(i.Item)) && i.Item.GetX == item.Item.GetX && i.Item.GetY == item.Item.GetY)
            .OrderBy(i => i.Item.GetZ)
            .ToList();

    public ICollection<IWiredItem> GetConditions(IWiredItem item) =>
        _wiredItems.Values
            .Where(i => IsCondition(i.Item) && i.Item.GetX == item.Item.GetX && i.Item.GetY == item.Item.GetY)
            .ToList();

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
            Console.WriteLine($"[WIRED] Error deserializing wired data for item {newBox.Item.Id}: {ex.Message}");
        }
    }

    public bool AddBox(IWiredItem item)
    {
        if (!_wiredItems.TryAdd(item.Item.Id, item))
            return false;

        var key = CoordKey(item.Item.GetX, item.Item.GetY);
        _byCoord.AddOrUpdate(key,
            _ => new List<IWiredItem> { item },
            (_, list) => { lock (list) { list.Add(item); } return list; });

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
    public IEnumerable<IWiredItem> GetAllItems() => _wiredItems.Values;
    public bool TryRemove(int itemId) => _wiredItems.TryRemove(itemId, out _);
    public bool TryGet(int id, out IWiredItem item) => _wiredItems.TryGetValue(id, out item);
    public bool IsTrigger(Item item) => item.GetBaseItem().InteractionType == InteractionType.WIRED_TRIGGER;
    public bool IsEffect(Item item) => item.GetBaseItem().InteractionType == InteractionType.WIRED_EFFECT;
    public bool IsCondition(Item item) => item.GetBaseItem().InteractionType == InteractionType.WIRED_CONDITION;
    public bool IsAddon(Item item) => item.GetBaseItem().InteractionType == InteractionType.WIRED_ADDON;
    public bool IsSelector(Item item) => item.GetBaseItem().WiredType.ToString().StartsWith("Selector") || item.GetBaseItem().WiredType.ToString().StartsWith("EffectUsers");
}
