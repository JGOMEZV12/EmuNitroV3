using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class SelectorUserAllBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.SelectorUserAll;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public SelectorUserAllBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket packet)
        {
            int unknown = packet.PopInt();
            string data = packet.PopString();
            this.StringData = data;
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(StringData ?? "");
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] @params)
        {
            if (@params.Length == 0 || !(@params[0] is WiredContext context))
                return false;

            context.SelectedUsers.Clear();
            foreach (var user in Instance.GetRoomUserManager().GetRoomUsers())
            {
                if (user.GetClient()?.GetHabbo() != null)
                    context.SelectedUsers.Add(user.GetClient().GetHabbo());
            }

            return true;
        }
    }
}
