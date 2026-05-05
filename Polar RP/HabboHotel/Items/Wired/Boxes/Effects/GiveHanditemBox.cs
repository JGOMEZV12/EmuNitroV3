using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using System;
using System.Collections.Concurrent;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class GiveHanditemBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.EffectGiveHanditem;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public GiveHanditemBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            StringData = "0";
        }

        public void HandleSave(ClientPacket packet)
        {
            int unknown = packet.PopInt();
            int handItemId = packet.PopInt();
            StringData = handItemId.ToString();
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(1);
            packet.WriteInteger(int.Parse(StringData));
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
        }

        public bool Execute(params object[] @params)
        {
            if (@params == null || @params.Length == 0) return false;

            Habbo player = @params[0] as Habbo;
            if (player == null) return false;

            RoomUser user = Instance.GetRoomUserManager().GetRoomUserByHabbo(player.Id);
            if (user == null) return false;

            int handItem = int.Parse(StringData);
            user.CarryItem(handItem);

            return true;
        }
    }
}
