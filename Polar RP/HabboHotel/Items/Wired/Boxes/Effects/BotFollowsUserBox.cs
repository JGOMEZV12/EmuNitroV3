using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Incoming;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class BotFollowsUserBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.EffectBotFollowsUserBox; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public BotFollowsUserBox(Room Instance, Item Item)
        {
            this.Instance = Instance;
            this.Item = Item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket Packet)
        {
            int IntCount = Packet.PopInt();
            int Mode = Packet.PopInt();   // 0=stop, 1=follow
            int UserSource = Packet.PopInt();
            int BotSource = Packet.PopInt();
            string BotName = Packet.PopString();

            this.StringData = Mode + ";" + UserSource + ";" + BotSource + ";" + BotName;
        }

        public void Serialize(ServerPacket Packet)
        {
            string botName = "";
            int mode = 0;
            int userSource = 0;
            int botSource = 0;

            if (!string.IsNullOrEmpty(this.StringData))
            {
                string[] parts = this.StringData.Split(';');
                if (parts.Length == 4)
                {
                    mode = int.Parse(parts[0]);
                    userSource = int.Parse(parts[1]);
                    botSource = int.Parse(parts[2]);
                    botName = parts[3];
                }
            }

            Packet.WriteBoolean(false);
            Packet.WriteInteger(5);
            Packet.WriteInteger(0);
            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString(botName);
            Packet.WriteInteger(3);
            Packet.WriteInteger(mode);
            Packet.WriteInteger(userSource);
            Packet.WriteInteger(botSource);
            Packet.WriteInteger(1);
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            Packet.WriteInteger(0);
            Packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params)
        {
            if (string.IsNullOrEmpty(this.StringData)) return false;

            string[] parts = this.StringData.Split(';');
            if (parts.Length != 4) return false;

            int mode = int.Parse(parts[0]);
            string botName = parts[3];

            if (Params != null && Params.Length > 0 && Params[0] is Habbo player)
            {
                RoomUser human = Instance.GetRoomUserManager().GetRoomUserByHabbo(player.Id);
                if (human == null) return false;

                RoomUser bot = Instance.GetRoomUserManager().GetBotByName(botName);
                if (bot == null) return false;

                if (mode == 1)
                {
                    bot.BotData.ForcedUserTargetMovement = player.Id;
                    if (bot.IsWalking) bot.ClearMovement(true);
                    bot.MoveTo(human.X, human.Y);
                }
                else
                {
                    bot.BotData.ForcedUserTargetMovement = 0;
                    if (bot.IsWalking) bot.ClearMovement(true);
                }
            }
            return true;
        }
    }
}
