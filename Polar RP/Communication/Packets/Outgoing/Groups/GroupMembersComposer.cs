using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Users;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Cache;

namespace Polar.Communication.Packets.Outgoing.Groups
{
    internal class GroupMembersComposer : ServerPacket
    {
        public Group Group { get; }
        public List<int> Members { get; }
        public int MembersCount { get; }
        public int Page { get; }
        public bool Admin { get; }
        public int ReqType { get; }
        public string SearchVal { get; }
        public GroupMembersComposer(Group group, List<int> members, int membersCount, int page, bool admin, int reqType, string searchVal)
            : base(ServerPacketHeader.GroupMembersMessageComposer)
        {
            this.Group = group;
            this.Members = members;
            this.MembersCount = membersCount;
            this.Page = page;
            this.Admin = admin;
            this.ReqType = reqType;
            this.SearchVal = searchVal;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            //Members = Members.Where(x => PolarEnvironment.GetGame().GetCacheManager().GenerateUser(x) != null).ToList();

            packet.WriteInteger(Group.Id);
            packet.WriteString(Group.Name);
            packet.WriteInteger(Group.RoomId);
            packet.WriteString(Group.Badge);
            packet.WriteInteger(MembersCount);
            packet.WriteInteger(Members.Count);

            if (MembersCount > 0)
            {
                foreach (int UserId in Members)
                {
                    if (UserId == 0)
                    {
                        HoloRPOwner(packet);
                        continue;
                    }

                    UserCache Data = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(UserId);

                    packet.WriteInteger(Group.CreatorId == Data.Id ? 0 : Group.IsAdmin(Data.Id) ? 1 : Group.IsMember(Data.Id) ? 2 : 3);
                    packet.WriteInteger(Data.Id);
                    packet.WriteString(Data.Username);
                    packet.WriteString(Data.Look);
                    packet.WriteString(string.Empty);
                }
            }
            packet.WriteBoolean(Admin);
            packet.WriteInteger(14);
            packet.WriteInteger(Page);
            packet.WriteInteger(ReqType);
            packet.WriteString(SearchVal);
        }

        public void HoloRPOwner(ServerPacket packet)
        {
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteString("HoloRP");
            packet.WriteString("ch-3032-110-1408.cp-3204-1.ha-3129-100.sh-290-110.hr-831-37.he-1604-63.fa-1206-1325.lg-270-100.hd-180-2.cc-3039-100");
            packet.WriteString(string.Empty);
        }
    }
}