using Polar.HabboHotel.Items.Wired;
using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Rooms.Games.Teams;
using Polar.Communication.Packets.Incoming;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class AddActorToTeamBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.EffectAddActorToTeam; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public AddActorToTeamBox(Room Instance, Item Item)
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
            if (Params.Length == 0 || Instance == null || String.IsNullOrEmpty(this.StringData))
                return false;

            Habbo Player = (Habbo)Params[0];
            if (Player == null)
                return false;

            RoomUser User = Instance.GetRoomUserManager().GetRoomUserByHabbo(Player.Id);
            if (User == null)
                return false;

            TEAM ToJoin = (int.Parse(this.StringData) == 1 ? TEAM.RED :
                           int.Parse(this.StringData) == 2 ? TEAM.GREEN :
                           int.Parse(this.StringData) == 3 ? TEAM.BLUE :
                           int.Parse(this.StringData) == 4 ? TEAM.YELLOW : TEAM.NONE);

            TeamManager Team = Instance.GetTeamManagerForFreeze();
            if (Team != null)
            {
                if (Team.CanEnterOnTeam(ToJoin))
                {
                    if (User.Team != TEAM.NONE)
                        Team.OnUserLeave(User);

                    User.Team = ToJoin;
                    Team.AddUser(User);

                    if (User.GetClient().GetHabbo().Effects().CurrentEffect != Convert.ToInt32(ToJoin + 39))
                        User.GetClient().GetHabbo().Effects().ApplyEffect(Convert.ToInt32(ToJoin + 39));
                }
            }
            return true;
        }
    }
}
