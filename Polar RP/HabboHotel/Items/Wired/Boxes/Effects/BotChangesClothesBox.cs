using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.Database.Interfaces;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class BotChangesClothesBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.EffectBotChangesClothesBox; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public BotChangesClothesBox(Room Instance, Item Item)
        {
            this.Instance = Instance;
            this.Item = Item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket Packet)
        {
            int IntCount = Packet.PopInt();   // cantidad de ints
            int BotSource = Packet.PopInt();   // botSource
            string ChatConfig = Packet.PopString(); // "botName\tbotLook"

            if (!ChatConfig.Contains("\t")) return;
            string[] parts = ChatConfig.Split('\t');
            if (parts.Length != 2) return;

            this.StringData = BotSource + ";" + parts[0] + ";" + parts[1];
        }

        public void Serialize(ServerPacket Packet)
        {
            string botName = "";
            string botLook = "";
            int botSource = 0;

            if (!string.IsNullOrEmpty(this.StringData))
            {
                string[] parts = this.StringData.Split(';');
                if (parts.Length == 3)
                {
                    botSource = int.Parse(parts[0]);
                    botName = parts[1];
                    botLook = parts[2];
                }
            }

            Packet.WriteBoolean(false);
            Packet.WriteInteger(5);
            Packet.WriteInteger(0);
            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString(botName + "\t" + botLook);
            Packet.WriteInteger(1);
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
            if (parts.Length != 3) return false;

            string botName = parts[1];
            string botLook = parts[2];

            RoomUser botUser = this.Instance.GetRoomUserManager().GetBotByName(botName);
            if (botUser == null) return false;

            botUser.BotData.Look = botLook;
            botUser.BotData.Gender = "M";

            Instance.SendMessage(new UserChangeComposer(botUser.VirtualId, botUser.BotData));

            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE `bots` SET `look`=@look, `gender`=@gender WHERE `id`=@id LIMIT 1");
                dbClient.AddParameter("look", botUser.BotData.Look);
                dbClient.AddParameter("gender", botUser.BotData.Gender);
                dbClient.AddParameter("id", botUser.BotData.Id);
                dbClient.RunQuery();
            }
            return true;
        }
    }
}
