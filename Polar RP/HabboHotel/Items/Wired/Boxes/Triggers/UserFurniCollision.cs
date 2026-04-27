using System.Collections.Generic;
using Polar.Communication.Packets.Outgoing;
using System.Collections.Concurrent;
using System.Linq;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Items.Wired;
using Polar.HabboHotel.Items;
using Polar.Communication.Packets.Incoming;

namespace Polar.HabboHotel.Items.Wired.Boxes.Triggers;

internal class UserFurniCollision : IWiredItem
{
    public UserFurniCollision(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        StringData = "";
        SetItems = new();
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }

    public WiredBoxType Type => WiredBoxType.TriggerUserFurniCollision;

    public ConcurrentDictionary<int, Item> SetItems { get; set; }
    public string StringData { get; set; }
    public bool BoolData { get; set; }
    public string ItemsData { get; set; }

            public void HandleSave(ClientPacket packet)
        {
            int paramsCount = packet.PopInt();
            for (int i = 0; i < paramsCount; i++) packet.PopInt();

            this.StringData = packet.PopString();

            if (this.SetItems != null) this.SetItems.Clear();
            int itemsCount = packet.PopInt();
            for (int i = 0; i < itemsCount; i++)
            {
                Item item = Instance.GetRoomItemHandler().GetItem(packet.PopInt());
                if (item != null) this.SetItems.TryAdd(item.Id, item);
            }

            int delay = packet.PopInt();
            if (this is IWiredCycle cycle) cycle.Delay = delay;
        }


                                public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(100);
            packet.WriteInteger(SetItems?.Count ?? 0);
            foreach (var item in SetItems?.Values.ToList() ?? new List<Item>()) packet.WriteInteger(item.Id);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(StringData ?? "");
            packet.WriteInteger(0); // Params count
            packet.WriteInteger(0); // Categorical
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
        }
        public bool Execute(params object[] @params)
    {
        // FIX: Validar player e item ANTES de llamar OnEvent
        var player = (Habbo)@params[0];
        if (player == null)
            return false;

        var item = (Item)@params[1];
        if (item == null)
            return false;

        var effects = Instance.GetWired().GetEffects(this);
        var conditions = Instance.GetWired().GetConditions(this);

        foreach (var condition in conditions.ToList())
        {
            if (!condition.Execute(player))
                return false;

            if (Instance != null)
                Instance.GetWired().OnEvent(condition.Item);
        }

        // FIX: Any() en lugar de .Count() > 0
        var hasRandomEffectAddon = effects.Any(x => x.Type == WiredBoxType.AddonRandomEffect);
        if (hasRandomEffectAddon)
        {
            // FIX: null-check en randomBox antes de ejecutar
            var randomBox = effects.FirstOrDefault(x => x.Type == WiredBoxType.AddonRandomEffect);
            if (randomBox == null || !randomBox.Execute())
                return false;

            var selectedBox = Instance.GetWired().GetRandomEffect(effects.ToList());
            if (selectedBox == null || !selectedBox.Execute())
                return false;

            if (Instance != null)
            {
                Instance.GetWired().OnEvent(randomBox.Item);
                Instance.GetWired().OnEvent(selectedBox.Item);
            }
        }
        else
        {
            foreach (var effect in effects.ToList())
            {
                if (!effect.Execute(player))
                    return false;

                if (Instance != null)
                    Instance.GetWired().OnEvent(effect.Item);
            }
        }

        // FIX: OnEvent movido al final, después de todas las validaciones
        Instance.GetWired().OnEvent(Item);

        return true;
    }
}
