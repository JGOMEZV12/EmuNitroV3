using Polar.HabboHotel.Items.Wired;
using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class ShowMessageBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.EffectShowMessage; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public ShowMessageBox(Room Instance, Item Item)
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
            if (Params == null || Params.Length == 0) return false;
            Habbo Player = (Habbo)Params[0];
            if (Player == null || Player.GetClient() == null || string.IsNullOrWhiteSpace(StringData)) return false;
            if (Player.CurrentRoom == null) return false;
            RoomUser User = Player.CurrentRoom.GetRoomUserManager().GetRoomUserByHabbo(Player.Id);
            if (User == null) return false;
            string Message = StringData;
            if (StringData.Contains("%USERNAME%")) Message = Message.Replace("%USERNAME%", Player.Username);
            if (StringData.Contains("%ROOMNAME%")) Message = Message.Replace("%ROOMNAME%", Player.CurrentRoom.Name);
            if (StringData.Contains("%USERCOUNT%")) Message = Message.Replace("%USERCOUNT%", Player.CurrentRoom.UserCount.ToString());
            if (StringData.Contains("%USERSONLINE%")) Message = Message.Replace("%USERSONLINE%", PolarEnvironment.GetGame().GetClientManager().Count.ToString());
            Message = Message.Replace("{username}", Player.Username);
            if (Player.GetClient().GetRoleplay() != null)
            {
                var rp = Player.GetClient().GetRoleplay();
                Message = Message.Replace("{job}", Polar.HabboHotel.Groups.GroupManager.GetJob(rp.JobId)?.Name ?? "Ninguno");
                Message = Message.Replace("{gang}", Polar.HabboHotel.Groups.GroupManager.GetGang(rp.GangId)?.Name ?? "Ninguna");
                Message = Message.Replace("{health}", rp.CurHealth.ToString());
                Message = Message.Replace("{maxhealth}", rp.MaxHealth.ToString());
                Message = Message.Replace("{armor}", rp.Armor.ToString());
                Message = Message.Replace("{energy}", rp.CurEnergy.ToString());
                Message = Message.Replace("{maxenergy}", rp.MaxEnergy.ToString());
                Message = Message.Replace("{hunger}", rp.Hunger.ToString());
                Message = Message.Replace("{hygiene}", rp.Hygiene.ToString());
                Message = Message.Replace("{poop}", rp.Poop.ToString());
                Message = Message.Replace("{level}", rp.Level.ToString());
                Message = Message.Replace("{exp}", rp.LevelEXP.ToString());
                Message = Message.Replace("{money}", Player.Credits.ToString());
                Message = Message.Replace("{bank}", rp.BankChequings.ToString());
                Message = Message.Replace("{intelligence}", rp.Intelligence.ToString());
                Message = Message.Replace("{strength}", rp.Strength.ToString());
                Message = Message.Replace("{stamina}", rp.Stamina.ToString());
            }
            Player.GetClient().SendMessage(new WhisperComposer(User.VirtualId, Message, 0, 34));
            return true;
        }
    }
}