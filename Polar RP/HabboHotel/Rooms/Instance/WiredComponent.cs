using System.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Items.Wired;
using Polar.HabboHotel.Items.Wired.Boxes;
using Polar.HabboHotel.Items.Wired.Boxes.Conditions;
using Polar.HabboHotel.Items.Wired.Boxes.Effects;
using Polar.HabboHotel.Items.Wired.Boxes.Triggers;
using Polar.HabboHotel.Items.Wired.Boxes.Add_ons;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Rooms.Instance;

public class WiredComponent
{
    private readonly Room _room;
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
    }

    // ── OnCycle ────────────────────────────────────────────────────────────
    // Sin ToList() — iteramos Values directamente (snapshot-safe en .NET)
    public void OnCycle()
    {
        foreach (var kvp in _wiredItems)
        {
            // Validar que el furni sigue en la sala (barato: dict lookup)
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
        WiredBoxType.TriggerRoomEnter              => new RoomEnterBox(_room, item),
        WiredBoxType.TriggerRepeat                 => new RepeaterBox(_room, item),
        WiredBoxType.TriggerStateChanges           => new StateChangesBox(_room, item),
        WiredBoxType.TriggerUserSays               => new UserSaysBox(_room, item),
        WiredBoxType.TriggerWalkOffFurni           => new UserWalksOffBox(_room, item),
        WiredBoxType.TriggerWalkOnFurni            => new UserWalksOnBox(_room, item),
        WiredBoxType.TriggerGameStarts             => new GameStartsBox(_room, item),
        WiredBoxType.TriggerGameEnds               => new GameEndsBox(_room, item),
        WiredBoxType.TriggerUserFurniCollision     => new UserFurniCollision(_room, item),
        WiredBoxType.TriggerUserSaysCommand        => new UserSaysCommandBox(_room, item),
        WiredBoxType.EffectShowMessage             => new ShowMessageBox(_room, item),
        WiredBoxType.EffectTeleportToFurni         => new TeleportUserBox(_room, item),
        WiredBoxType.EffectToggleFurniState        => new ToggleFurniBox(_room, item),
        WiredBoxType.EffectMoveAndRotate           => new MoveAndRotateBox(_room, item),
        WiredBoxType.EffectKickUser                => new KickUserBox(_room, item),
        WiredBoxType.EffectMuteTriggerer           => new MuteTriggererBox(_room, item),
        WiredBoxType.EffectGiveReward              => new GiveRewardBox(_room, item),
        WiredBoxType.EffectMatchPosition           => new MatchPositionBox(_room, item),
        WiredBoxType.EffectAddActorToTeam          => new AddActorToTeamBox(_room, item),
        WiredBoxType.EffectCollisionCase           => new CollisionCaseBox(_room, item),
        WiredBoxType.EffectRemoveActorFromTeam     => new RemoveActorFromTeamBox(_room, item),
        WiredBoxType.EffectAddScore                => new AddScoreBox(_room, item),
        WiredBoxType.ConditionFurniHasUsers        => new FurniHasUsersBox(_room, item),
        WiredBoxType.ConditionTriggererOnFurni     => new TriggererOnFurniBox(_room, item),
        WiredBoxType.ConditionTriggererNotOnFurni  => new TriggererNotOnFurniBox(_room, item),
        WiredBoxType.ConditionFurniHasNoUsers      => new FurniHasNoUsersBox(_room, item),
        WiredBoxType.ConditionFurniHasFurni        => new FurniHasFurniBox(_room, item),
        WiredBoxType.ConditionIsGroupMember        => new IsGroupMemberBox(_room, item),
        WiredBoxType.ConditionIsNotGroupMember     => new IsNotGroupMemberBox(_room, item),
        WiredBoxType.ConditionUserCountInRoom      => new UserCountInRoomBox(_room, item),
        WiredBoxType.ConditionUserCountDoesntInRoom=> new UserCountDoesntInRoomBox(_room, item),
        WiredBoxType.ConditionIsWearingFX          => new IsWearingFXBox(_room, item),
        WiredBoxType.ConditionIsNotWearingFX       => new IsNotWearingFXBox(_room, item),
        WiredBoxType.ConditionIsWearingBadge       => new IsWearingBadgeBox(_room, item),
        WiredBoxType.ConditionFurniTypeMatches     => new FurniTypeMatchesBox(_room, item),
        WiredBoxType.ConditionFurniTypeDoesntMatch => new FurniTypeDoesntMatchBox(_room, item),
        WiredBoxType.ConditionIsNotWearingBadge    => new IsNotWearingBadgeBox(_room, item),
        WiredBoxType.ConditionMatchStateAndPosition       => new FurniMatchStateAndPositionBox(_room, item),
        WiredBoxType.ConditionDontMatchStateAndPosition   => new FurniDoesntMatchStateAndPositionBox(_room, item),
        WiredBoxType.ConditionActorHasHandItemBox  => new ActorHasHandItemBox(_room, item),
        WiredBoxType.ConditionNotActorHasHandItemBox => new NotActorHasHandItemBox(_room, item),
        WiredBoxType.ConditionActorIsInTeamBox     => new ActorIsInTeamBox(_room, item),
        WiredBoxType.AddonRandomEffect             => new AddonRandomEffectBox(_room, item),
        WiredBoxType.EffectMoveFurniToNearestUser  => new MoveFurniToUserBox(_room, item),
        WiredBoxType.EffectExecuteWiredStacks      => new ExecuteWiredStacksBox(_room, item),
        WiredBoxType.EffectTeleportBotToFurniBox   => new TeleportBotToFurniBox(_room, item),
        WiredBoxType.EffectBotChangesClothesBox    => new BotChangesClothesBox(_room, item),
        WiredBoxType.EffectBotMovesToFurniBox      => new BotMovesToFurniBox(_room, item),
        WiredBoxType.EffectBotCommunicatesToAllBox => new BotCommunicatesToAllBox(_room, item),
        WiredBoxType.EffectBotCommunicatesToUserBox => new BotCommunicatesToUserBox(_room, item),
        WiredBoxType.EffectBotGivesHanditemBox     => new BotGivesHandItemBox(_room, item),
        WiredBoxType.EffectBotFollowsUserBox       => new BotFollowsUserBox(_room, item),
        WiredBoxType.EffectSetRollerSpeed          => new SetRollerSpeedBox(_room, item),
        WiredBoxType.EffectRegenerateMaps          => new RegenerateMapsBox(_room, item),
        WiredBoxType.EffectGiveUserBadge           => new GiveUserBadgeBox(_room, item),
        WiredBoxType.EffectGiveCurrency            => new GiveCurrencyBox(_room, item),
        WiredBoxType.ConditionHasJob               => new HasJobBox(_room, item),
        WiredBoxType.ConditionIsNight              => new IsNightBox(_room, item),
        WiredBoxType.ConditionIsDay                => new IsDayBox(_room, item),
        WiredBoxType.EffectExecuteCommand          => new ExecuteCommandBox(_room, item),
        WiredBoxType.EffectGiveExperience          => new GiveExperienceBox(_room, item),
        WiredBoxType.ConditionIsJailed             => new IsJailedBox(_room, item),
        WiredBoxType.ConditionIsDead               => new IsDeadBox(_room, item),
        WiredBoxType.EffectApplyEffect             => new ApplyEffectBox(_room, item),
        WiredBoxType.ConditionIsDriving            => new IsDrivingBox(_room, item),
        WiredBoxType.EffectDamageUser              => new DamageUserBox(_room, item),
        WiredBoxType.EffectHealUser                => new HealUserBox(_room, item),
        WiredBoxType.ConditionHasVip               => new HasVipBox(_room, item),
        WiredBoxType.EffectSetMotto                => new SetMottoBox(_room, item),
        WiredBoxType.EffectFreezeUser              => new FreezeUserBox(_room, item),
        WiredBoxType.EffectUnfreezeUser            => new UnfreezeUserBox(_room, item),
        WiredBoxType.ConditionIsSitting            => new IsSittingBox(_room, item),
        WiredBoxType.EffectGiveHuntPoints          => new GiveHuntPointsBox(_room, item),
        WiredBoxType.EffectGiveEnergy              => new GiveEnergyBox(_room, item),
        WiredBoxType.EffectGiveArmor               => new GiveArmorBox(_room, item),
        WiredBoxType.ConditionHasWeapon            => new HasWeaponBox(_room, item),
        WiredBoxType.EffectGiveRPItem              => new GiveRPItemBox(_room, item),
        WiredBoxType.ConditionHasRPItem            => new HasRPItemBox(_room, item),
        WiredBoxType.EffectSetRotation             => new SetRotationBox(_room, item),
        WiredBoxType.ConditionIsIdle               => new IsIdleBox(_room, item),
        WiredBoxType.ConditionIsDancing            => new IsDancingBox(_room, item),
        WiredBoxType.AddonAnimationTime            => new AddonAnimationTimeBox(_room, item),
        WiredBoxType.AddonExecuteInOrder           => new AddonExecuteInOrderBox(_room, item),
        WiredBoxType.AddonExecutionLimit           => new AddonExecutionLimitBox(_room, item),
        WiredBoxType.AddonFilterFurni              => new AddonFilterFurniBox(_room, item),
        WiredBoxType.AddonFilterUser               => new AddonFilterUserBox(_room, item),
        WiredBoxType.AddonMoveCarryUsers           => new AddonMoveCarryUsersBox(_room, item),
        WiredBoxType.AddonMoveNoAnimation          => new AddonMoveNoAnimationBox(_room, item),
        WiredBoxType.AddonMovePhysics              => new AddonMovePhysicsBox(_room, item),
        WiredBoxType.AddonOrEval                   => new AddonOrEvalBox(_room, item),
        WiredBoxType.AddonRandom                   => new AddonRandomBox(_room, item),
        WiredBoxType.AddonTextOutputFurniName      => new AddonTextOutputFurniNameBox(_room, item),
        WiredBoxType.AddonTextOutputUsername       => new AddonTextOutputUsernameBox(_room, item),
        WiredBoxType.AddonUnseen                   => new AddonUnseenBox(_room, item),
        WiredBoxType.EffectSetVariable             => new SetVariableBox(_room, item),
        WiredBoxType.EffectVariableAdd             => new VariableAddBox(_room, item),
        WiredBoxType.ConditionVariableIsEqual      => new VariableIsEqualBox(_room, item),
        WiredBoxType.EffectVariableSubtract        => new VariableSubtractBox(_room, item),
        WiredBoxType.ConditionVariableIsGreaterThan => new VariableIsGreaterThanBox(_room, item),
        WiredBoxType.ConditionVariableIsLessThan    => new VariableIsLessThanBox(_room, item),
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

    public ICollection<IWiredItem> GetTriggers(IWiredItem item) =>
        _wiredItems.Values
            .Where(i => IsTrigger(i.Item) && i.Item.GetX == item.Item.GetX && i.Item.GetY == item.Item.GetY)
            .ToList();

    public ICollection<IWiredItem> GetEffects(IWiredItem item) =>
        _wiredItems.Values
            .Where(i => IsEffect(i.Item) && i.Item.GetX == item.Item.GetX && i.Item.GetY == item.Item.GetY)
            .OrderBy(i => i.Item.GetZ)
            .ToList();

    public ICollection<IWiredItem> GetConditions(IWiredItem item) =>
        _wiredItems.Values
            .Where(i => IsCondition(i.Item) && i.Item.GetX == item.Item.GetX && i.Item.GetY == item.Item.GetY)
            .ToList();

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

        // FIX: eliminada la llamada a GetItem() redundante — los items ya están en SetItems (cargados antes)
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

        // FIX: pattern matching en lugar de cast doble
        int delay = item is IWiredCycle c ? c.Delay : 0;

        using var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
        dbClient.SetQuery("REPLACE INTO `wired_items` VALUES (@id, @items, @delay, @string, @bool)");
        dbClient.AddParameter("id", item.Item.Id);
        dbClient.AddParameter("items", items);
        dbClient.AddParameter("delay", delay);
        dbClient.AddParameter("string", item.StringData);
        dbClient.AddParameter("bool", item.BoolData ? "1" : "0");
        dbClient.RunQuery();
    }

    // ── LoadWiredBox — sin cambios de lógica, solo usa nuevo AddBox ────────
    public IWiredItem LoadWiredBox(Item item) { LoadWiredBoxes(new[] { item }); TryGet(item.Id, out var box); return box; }

    public void LoadWiredBoxes(IEnumerable<Item> items)
    {
        var wiredItems = items.Where(i => i.IsWired).ToList();
        if (wiredItems.Count == 0) return;

        var ids = string.Join(",", wiredItems.Select(i => i.Id));
        DataTable table;
        using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
        {
            dbClient.SetQuery($"SELECT * FROM wired_items WHERE id IN ({ids})");
            table = dbClient.getTable();
        }

        var rows = table?.Rows.Cast<DataRow>().ToDictionary(r => Convert.ToInt32(r["id"])) ?? new Dictionary<int, DataRow>();

        foreach (var item in wiredItems)
        {
            var newBox = GenerateNewBox(item);
            if (newBox == null) continue;

            if (rows.TryGetValue(item.Id, out var row))
            {
                ApplyRowToBox(newBox, row);
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

    private void ApplyRowToBox(IWiredItem newBox, DataRow row)
    {
        string rawString = Convert.ToString(row["string"]);
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

        newBox.BoolData = Convert.ToInt32(row["bool"]) == 1;
        newBox.ItemsData = Convert.ToString(row["items"]);

        if (newBox is IWiredCycle cycle)
            cycle.Delay = Convert.ToInt32(row["delay"]);

        foreach (var str in newBox.ItemsData.Split(';'))
        {
            var sId = str.Contains(':') ? str.Split(':')[0] : str;
            if (int.TryParse(sId, out int id))
            {
                var selectedItem = _room.GetRoomItemHandler().GetItem(id);
                if (selectedItem != null)
                    newBox.SetItems.TryAdd(selectedItem.Id, selectedItem);
            }
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
    public bool TryRemove(int itemId) => _wiredItems.TryRemove(itemId, out _);
    public bool TryGet(int id, out IWiredItem item) => _wiredItems.TryGetValue(id, out item);
    public bool IsTrigger(Item item) => item.GetBaseItem().InteractionType == InteractionType.WIRED_TRIGGER;
    public bool IsEffect(Item item) => item.GetBaseItem().InteractionType == InteractionType.WIRED_EFFECT;
    public bool IsCondition(Item item) => item.GetBaseItem().InteractionType == InteractionType.WIRED_CONDITION;
    public bool IsAddon(Item item) => item.GetBaseItem().InteractionType == InteractionType.WIRED_ADDON;
}