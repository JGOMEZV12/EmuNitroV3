using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class SelectorUserRandomBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.SelectorUserRandom;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public SelectorUserRandomBox(Room instance, Item item)
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

            var users = Instance.GetRoomUserManager().GetRoomUsers().Where(u => u.GetClient()?.GetHabbo() != null).ToList();
            if (users.Count == 0) return true;

            context.SelectedUsers.Clear();
            context.SelectedUsers.Add(users[Random.Shared.Next(users.Count)].GetClient().GetHabbo());

            return true;
        }
    }
}
