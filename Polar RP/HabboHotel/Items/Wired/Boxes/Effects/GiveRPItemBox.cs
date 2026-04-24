using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Collections.Concurrent;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class GiveRPItemBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.EffectGiveRPItem; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public GiveRPItemBox(Room instance, Item item)
        {
            this.Instance = instance;
            this.Item = item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket Packet)
        {
            int Unknown = Packet.PopInt();
            string ItemName = Packet.PopString(); // weed, cocaine, medikits, etc.
            string Amount = Packet.PopString();
            this.StringData = ItemName + ";" + Amount;
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
            Packet.WriteInteger(0);
            if (this is IWiredCycle)
            {
                Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
                Packet.WriteInteger(0);
                Packet.WriteInteger(((IWiredCycle)this).Delay);
            }
            else
            {
                Packet.WriteInteger(0);
                Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
                Packet.WriteInteger(0);
            }
        }
        public bool Execute(params object[] Params)
        {
            if (Params == null || Params.Length == 0)
                return false;

            Habbo Player = (Habbo)Params[0];
            if (Player == null || Player.GetClient() == null || Player.GetClient().GetRoleplay() == null)
                return false;

            if (string.IsNullOrEmpty(StringData))
                return false;

            string[] data = StringData.Split(';');
            string itemName = data[0].ToLower();
            int amount = int.Parse(data[1]);

            var rp = Player.GetClient().GetRoleplay();

            switch (itemName)
            {
                case "weed": rp.Weed += amount; break;
                case "cocaine": rp.Cocaine += amount; break;
                case "medicina": rp.Medicina += amount; break;
                case "caramelos": rp.Caramelos += amount; break;
                case "heroina": rp.Heroina += amount; break;
            }

            Player.GetClient().SendWhisper("Has recibido " + amount + " de " + itemName + " vía Wired.", 1);
            return true;
        }
    }
}
