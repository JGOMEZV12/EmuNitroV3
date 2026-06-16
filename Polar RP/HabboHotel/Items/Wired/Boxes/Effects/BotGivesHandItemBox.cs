using Polar.HabboHotel.Items.Wired;
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
