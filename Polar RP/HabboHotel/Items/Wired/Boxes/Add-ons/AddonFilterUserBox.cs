using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Items.Wired.Boxes.Add_ons
{
    class AddonFilterUserBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.AddonFilterUser;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }


        public AddonFilterUserBox(Room instance, Item item)
        {
            this.Instance = instance;
            this.Item = item;
            this.SetItems = new();
            this.StringData = "";
        }

        public void HandleSave(ClientPacket Packet)
        {
            int unknown = Packet.PopInt();
            int mode = Packet.PopInt();
            this.StringData = mode.ToString();
        }

        
        public void Serialize(ServerPacket Packet)
        {
            Packet.WriteBoolean(false);
            Packet.WriteInteger(100);
            Packet.WriteInteger(SetItems.Count);
            foreach (Item Item in SetItems.Values.ToList())
            {
                Packet.WriteInteger(Item.Id);
            }
            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString(StringData);
            Packet.WriteInteger(0);
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            Packet.WriteInteger(0);
        }
        public bool Execute(params object[] @params)
        {
            if (@params.Length == 0 || !(@params[0] is WiredContext context))
                return true;

            if (SetItems.Count == 0) return true;

            // Mode 0: In list, Mode 1: Not in list
            int.TryParse(StringData, out int mode);

            var targetItems = SetItems.Values.ToList();
            var usersOnItems = new List<Habbo>();

            foreach (var item in targetItems)
            {
                var users = Instance.GetGameMap().GetRoomUsers(new System.Drawing.Point(item.GetX, item.GetY));
                foreach (var user in users)
                {
                    if (user.GetClient()?.GetHabbo() != null)
                        usersOnItems.Add(user.GetClient().GetHabbo());
                }
            }

            if (mode == 0)
            {
                context.SelectedUsers = context.SelectedUsers.Where(u => usersOnItems.Contains(u)).ToList();
            }
            else
            {
                context.SelectedUsers = context.SelectedUsers.Where(u => !usersOnItems.Contains(u)).ToList();
            }

            return true;
        }
    }
}