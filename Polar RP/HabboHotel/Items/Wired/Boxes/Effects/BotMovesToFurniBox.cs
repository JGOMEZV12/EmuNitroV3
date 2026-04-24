using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Incoming;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class BotMovesToFurniBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.EffectBotMovesToFurniBox; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public BotMovesToFurniBox(Room Instance, Item Item)
        {
            this.Instance = Instance;
            this.Item = Item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket Packet)
        {
            int IntCount = Packet.PopInt();
            int FurniSource = Packet.PopInt();
            int BotSource = Packet.PopInt();
            string BotName = Packet.PopString();

            if (this.SetItems.Count > 0) this.SetItems.Clear();

            int FurniCount = Packet.PopInt();
            for (int i = 0; i < FurniCount; i++)
            {
                Item selected = Instance.GetRoomItemHandler().GetItem(Packet.PopInt());
                if (selected != null)
                    SetItems.TryAdd(selected.Id, selected);
            }

            this.StringData = FurniSource + ";" + BotSource + ";" + BotName;
        }

        public void Serialize(ServerPacket Packet)
        {
            string botName = "";
            int furniSource = 0;
            int botSource = 0;

            if (!string.IsNullOrEmpty(this.StringData))
            {
                string[] parts = this.StringData.Split(';');
                if (parts.Length == 3)
                {
                    furniSource = int.Parse(parts[0]);
                    botSource = int.Parse(parts[1]);
                    botName = parts[2];
                }
            }

            Packet.WriteBoolean(false);
            Packet.WriteInteger(100); // max furni
            Packet.WriteInteger(SetItems.Count);
            foreach (Item i in SetItems.Values.ToList())
                Packet.WriteInteger(i.Id);

            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString(botName);
            Packet.WriteInteger(2);
            Packet.WriteInteger(furniSource);
            Packet.WriteInteger(botSource);
            Packet.WriteInteger(0);
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            Packet.WriteInteger(0);
            Packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params)
        {
            if (Params == null || Params.Length == 0 || String.IsNullOrEmpty(this.StringData))
                return false;

            RoomUser User = this.Instance.GetRoomUserManager().GetBotByName(this.StringData);
            if (User == null)
                return false;

            Random rand = new Random();
            List<Item> Items = SetItems.Values.ToList();
            Items = Items.OrderBy(x => rand.Next()).ToList();

            if (Items.Count == 0)
                return false;

            Item Item = Items.First();
            if (Item == null)
                return false;

            if (!Instance.GetRoomItemHandler().GetFloor.Contains(Item))
            {
                SetItems.TryRemove(Item.Id, out Item);

                if (Items.Contains(Item))
                    Items.Remove(Item);

                if (SetItems.Count == 0 || Items.Count == 0)
                    return false;

                Item = Items.First();
                if (Item == null)
                    return false;
            }

            if (this.Instance.GetGameMap() == null)
                return false;

            if (User.IsWalking)
                User.ClearMovement(true);

            User.BotData.ForcedMovement = true;
            User.BotData.TargetCoordinate = new Point(Item.GetX, Item.GetY);
            User.MoveTo(Item.GetX, Item.GetY);

            return true;
        }
    }
}
