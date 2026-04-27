using Polar.HabboHotel.Items.Wired;
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

                public void HandleSave(ClientPacket packet)
        {
            int paramsCount = packet.PopInt();
            for (int i = 0; i < paramsCount; i++) packet.PopInt();

            this.StringData = packet.PopString();

            if (this.SetItems != null) this.SetItems.Clear();
            int itemsCount = packet.PopInt();
            for (int i = 0; i < itemsCount; i++)
            {
                Item item = Instance.GetRoomItemHandler().GetItem(packet.PopInt());
                if (item != null) this.SetItems.TryAdd(item.Id, item);
            }

            int delay = packet.PopInt();
            if (this is IWiredCycle cycle) cycle.Delay = delay;
        }

                                public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(100);
            packet.WriteInteger(SetItems?.Count ?? 0);
            foreach (var item in SetItems?.Values.ToList() ?? new List<Item>()) packet.WriteInteger(item.Id);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(StringData ?? "");
            packet.WriteInteger(0); // Params count
            packet.WriteInteger(0); // Categorical
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(this is IWiredCycle cycle ? cycle.Delay : 0);
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
