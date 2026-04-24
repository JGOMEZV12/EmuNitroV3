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
        var unknown = packet.PopInt();
        var unknown2 = packet.PopString();
    }

    
        public void Serialize(ServerPacket Packet)
        {
            Packet.WriteBoolean(false);
            Packet.WriteInteger(100);
            Packet.WriteInteger(SetItems.Count);
            foreach (Item Item in SetItems.Values.ToList())
            {
                Packet.WriteInteger(Item.Id);
            }
            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString(StringData);

            Packet.WriteInteger(this is IWiredCycle ? 1 : 0);
            if (this is IWiredCycle)
            {
                IWiredCycle Cycle = (IWiredCycle)this;
                Packet.WriteInteger(Cycle.Delay);
            }
            Packet.WriteInteger(0);
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
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
