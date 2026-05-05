using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class BotCommunicatesToUserBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.EffectBotCommunicatesToUserBox; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public BotCommunicatesToUserBox(Room Instance, Item Item)
        {
            this.Instance = Instance;
            this.Item = Item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
            this.StringData = "";
            this.BoolData = false;
        }

        public void HandleSave(ClientPacket Packet)
        {
            int Unknown = Packet.PopInt();
            string Message = Packet.PopString();

            this.StringData = Message;
        }

        public void Serialize(ServerPacket Packet)
        {
            Packet.WriteBoolean(false);
            Packet.WriteInteger(5); // Max selection
            Packet.WriteInteger(0); // Item count
            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString(this.StringData);
            Packet.WriteInteger(0); // Params count
            Packet.WriteInteger(0); // Selection mode
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            Packet.WriteInteger(0); // Delay
            Packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params)
        {
            if (Params == null || Params.Length == 0 || string.IsNullOrEmpty(this.StringData))
                return false;

            Habbo Player = Params[0] as Habbo;
            if (Player == null || Player.GetClient() == null)
                return false;

            string[] Data = this.StringData.Split('\t');
            if (Data.Length != 2)
                return false;

            string BotName = Data[0];
            string Message = Data[1];

            RoomUser User = Instance.GetRoomUserManager().GetRoomUserByHabbo(Player.Id);
            if (User == null)
                return false;

            RoomUser Bot = Instance.GetRoomUserManager().GetBotByName(BotName);
            if (Bot == null)
                return false;

            Player.GetClient().SendWhisper(Bot.BotData.Name + ": " + Message);
            return true;
        }
    }
}
