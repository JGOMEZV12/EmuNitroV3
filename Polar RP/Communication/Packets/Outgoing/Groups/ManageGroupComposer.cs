using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;

namespace Polar.Communication.Packets.Outgoing.Groups
{
    internal class ManageGroupComposer : ServerPacket
    {
        public Group Group { get; }

        public ManageGroupComposer(Group Group)
            : base(ServerPacketHeader.ManageGroupMessageComposer)
        {
            this.Group = Group;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(0);
            packet.WriteBoolean(true);
            packet.WriteInteger(Group.Id);
            packet.WriteString(Group.Name);
            packet.WriteString(Group.Description);
            packet.WriteInteger(1);
            packet.WriteInteger(1);
            packet.WriteInteger(1);
            packet.WriteInteger(Group.GroupType == GroupType.OPEN ? 0 : Group.GroupType == GroupType.LOCKED ? 1 : 2);
            packet.WriteInteger(Group.AdminOnlyDeco);
            packet.WriteBoolean(false);
            packet.WriteString("");

            string FakeBadge = "b05114s06114";
            string[] BadgeSplit = null;

            if (Group.IsGang == false)
                BadgeSplit = FakeBadge.Replace("b", "").Split('s');
            else
                BadgeSplit = Group.Badge.Replace("b", "").Split('s');

            packet.WriteInteger(5);
            int Req = 5 - BadgeSplit.Length;
            int Final = 0;
            string[] array2 = BadgeSplit;
            for (int i = 0; i < array2.Length; i++)
            {
                string Symbol = array2[i];
                packet.WriteInteger((Symbol.Length >= 6) ? int.Parse(Symbol.Substring(0, 3)) : int.Parse(Symbol.Substring(0, 2)));
                packet.WriteInteger((Symbol.Length >= 6) ? int.Parse(Symbol.Substring(3, 2)) : int.Parse(Symbol.Substring(2, 2)));
                packet.WriteInteger(Symbol.Length < 5 ? 0 : Symbol.Length >= 6 ? int.Parse(Symbol.Substring(5, 1)) : int.Parse(Symbol.Substring(4, 1)));
            }

            while (Final != Req)
            {
                packet.WriteInteger(0);
                packet.WriteInteger(0);
                packet.WriteInteger(0);
                Final++;
            }          

            packet.WriteString(Group.Badge);
            packet.WriteInteger(Group.IsGang == false ? (Group.Members.Count + 1) : Group.Members.Count);
        }
    }
}
