using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Collections.Concurrent;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class GiveCurrencyBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.EffectGiveCurrency; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public GiveCurrencyBox(Room instance, Item item)
        {
            this.Instance = instance;
            this.Item = item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket Packet)
        {
            int Unknown = Packet.PopInt();
            string Amount = Packet.PopString(); // Cantidad
            string CurrencyType = Packet.PopString(); // 0: Credits, 1: Duckets, 2: Diamonds

            this.StringData = Amount + ";" + CurrencyType;
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
            if (Player == null || Player.GetClient() == null)
                return false;

            if (string.IsNullOrEmpty(StringData))
                return false;

            string[] data = StringData.Split(';');
            if (data.Length < 2) return false;

            if (!int.TryParse(data[0], out int amount)) return false;
            int type = int.Parse(data[1]);

            switch (type)
            {
                case 0: // Credits
                    Player.Credits += amount;
                    Player.GetClient().SendMessage(new Polar.Communication.Packets.Outgoing.Inventory.Purse.CreditBalanceComposer(Player.Credits));
                    break;
                case 1: // Duckets
                    Player.Duckets += amount;
                    Player.GetClient().SendMessage(new Polar.Communication.Packets.Outgoing.Inventory.Purse.HabboActivityPointNotificationComposer(Player.Duckets, amount));
                    break;
                case 2: // Diamonds
                    Player.Diamonds += amount;
                    Player.GetClient().SendMessage(new Polar.Communication.Packets.Outgoing.Inventory.Purse.HabboActivityPointNotificationComposer(Player.Diamonds, amount, 5));
                    break;
            }

            return true;
        }
    }
}
