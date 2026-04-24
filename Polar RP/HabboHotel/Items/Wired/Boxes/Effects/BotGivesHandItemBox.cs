using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class BotGivesHandItemBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.EffectBotGivesHanditemBox; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public BotGivesHandItemBox(Room Instance, Item Item)
        {
            this.Instance = Instance;
            this.Item = Item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket Packet)
        {
            int IntCount = Packet.PopInt();
            int ItemId = Packet.PopInt();   // hand item id
            int UserSource = Packet.PopInt();
            int BotSource = Packet.PopInt();
            string BotName = Packet.PopString();

            this.StringData = BotName + ";" + ItemId + ";" + UserSource + ";" + BotSource;
        }

        public void Serialize(ServerPacket Packet)
        {
            string botName = "";
            int itemId = 0;
            int userSource = 0;
            int botSource = 0;

            if (!string.IsNullOrEmpty(this.StringData))
            {
                string[] parts = this.StringData.Split(';');
                if (parts.Length == 4)
                {
                    botName = parts[0];
                    itemId = int.Parse(parts[1]);
                    userSource = int.Parse(parts[2]);
                    botSource = int.Parse(parts[3]);
                }
            }

            Packet.WriteBoolean(false);
            Packet.WriteInteger(5);
            Packet.WriteInteger(0);
            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString(botName);
            Packet.WriteInteger(3);
            Packet.WriteInteger(itemId);
            Packet.WriteInteger(userSource);
            Packet.WriteInteger(botSource);
            Packet.WriteInteger(0);
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            Packet.WriteInteger(0);
            Packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params)
        {
            if (string.IsNullOrEmpty(this.StringData)) return false;

            string[] parts = this.StringData.Split(';');
            if (parts.Length != 4) return false;

            string botName = parts[0];
            int drinkId = int.Parse(parts[1]);

            if (Params != null && Params.Length > 0 && Params[0] is Habbo player)
            {
                RoomUser actor = Instance.GetRoomUserManager().GetRoomUserByHabbo(player.Id);
                if (actor == null) return false;

                RoomUser bot = Instance.GetRoomUserManager().GetBotByName(botName);
                if (bot == null) return false;

                if (bot.BotData.TargetUser == 0)
                {
                    if (!Instance.GetGameMap().CanWalk(actor.SquareBehind.X, actor.SquareBehind.Y, false))
                        return false;

                    bot.CarryItem(drinkId);
                    bot.BotData.TargetUser = actor.HabboId;
                    bot.MoveTo(actor.SquareBehind.X, actor.SquareBehind.Y);
                }
            }
            return true;
        }
    }
}
