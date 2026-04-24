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
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;

namespace Polar.Communication.Packets.Incoming.Groups
{
    internal class UpdateForumSettingsMessageEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            int guild = Packet.PopInt();
            int whoCanRead = Packet.PopInt();
            int whoCanPost = Packet.PopInt();
            int whoCanThread = Packet.PopInt();
            int whoCanMod = Packet.PopInt();

            Group group = GroupManager.GetJob(guild);

            if (group == null)
                return;

            group.WhoCanRead = whoCanRead;
            group.WhoCanPost = whoCanPost;
            group.WhoCanThread = whoCanThread;
            group.WhoCanMod = whoCanMod;

            using (IQueryAdapter commitableQueryReactor = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                commitableQueryReactor.SetQuery(
                    "UPDATE groups SET who_can_read = @who_can_read, who_can_post = @who_can_post, who_can_thread = @who_can_thread, who_can_mod = @who_can_mod WHERE id = @group_id");
                commitableQueryReactor.AddParameter("group_id", group.Id);
                commitableQueryReactor.AddParameter("who_can_read", whoCanRead);
                commitableQueryReactor.AddParameter("who_can_post", whoCanPost);
                commitableQueryReactor.AddParameter("who_can_thread", whoCanThread);
                commitableQueryReactor.AddParameter("who_can_mod", whoCanMod);
                commitableQueryReactor.RunQuery();
            }

            Session.SendMessage(new GroupForumDataMessageComposer(group, Session));
        }
    }
}
