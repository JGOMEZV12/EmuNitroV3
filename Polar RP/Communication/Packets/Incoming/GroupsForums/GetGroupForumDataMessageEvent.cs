using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;

using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Guides;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Groups;
using Polar.Communication.Packets.Outgoing.Groups;

namespace Polar.Communication.Packets.Incoming.Groups
{
    internal class GetGroupForumDataMessageEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            int GroupId = Packet.PopInt();
            Group Group = GroupManager.GetJob(GroupId);

            if (Group == null || !Group.ForumEnabled)
                return;

            Session.SendMessage(new GroupForumDataMessageComposer(Group, Session));
        }
    }
}
