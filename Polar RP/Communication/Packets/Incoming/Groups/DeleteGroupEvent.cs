using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Turfs;
using Polar.HabboHotel.Cache;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Incoming.Groups
{
    internal class DeleteGroupEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            int GroupId = Packet.PopInt();

            if (GroupManager.GetJob(GroupId).IsGang == false)
            {
                Session.SendNotification("You cannot delete a corporation!");
                return;
            }

            Group Group = GroupManager.GetGang(GroupId);
            if (Group == null)
            {
                Session.SendNotification("Oops, we couldn't find that gang!");
                return;
            }

            if (Group.CreatorId != Session.GetHabbo().Id && !Session.GetHabbo().GetPermissions().HasRight("roleplay_corp_manager"))//Maybe a FUSE check for staff override?
            {
                Session.SendNotification("Oops, only the group owner can delete a group!");
                return;
            }

            if (Group.Members.Count >= PolarStaticGameSettings.GroupMemberDeletionLimit)
            {
                Session.SendNotification("Oops, your group exceeds the maximum amount of members (" + PolarStaticGameSettings.GroupMemberDeletionLimit + ") a group can exceed before being eligible for deletion. Seek assistance from a staff member.");
                return;
            }

            if (RoleplayManager.GenerateRoom(Group.RoomId, out var Room))
            {
                Room.Group = null;
                Room.RoomData.Group = null;
            }

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("UPDATE `rp_stats` SET `gang_id` = '0', `gang_rank` = '1', `gang_request` = '0' WHERE `gang_id` = '" + Group.Id + "'");
                dbClient.RunQuery("UPDATE `rooms` SET `group_id` = '0' WHERE `group_id` = '" + Group.Id + "' LIMIT 1");
                dbClient.RunQuery("DELETE FROM `rp_gangs` WHERE `id` = '" + Group.Id + "' LIMIT 1");
                dbClient.RunQuery("DELETE FROM `items_groups` WHERE `group_id` = '" + Group.Id + "'");
            }

            foreach (int Member in Group.Members.Keys)
            {
                GameClient Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Member);
                if (Client != null)
                {
                    Client.GetRoleplay().GangId = 0;
                    Client.GetRoleplay().GangRank = 0;
                    Client.GetRoleplay().GangRequest = 0;

                    Group NoGang = null;
                    //NoGang.AddNewMember(Client.GetHabbo().Id);

                    UserCache Junk = null;
                    PolarEnvironment.GetGame().GetCacheManager().TryRemoveUser(Member, out Junk);
                    PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Member);
                    NoGang.SendPackets(Client);
                }
            }

            foreach (var turf in TurfManager.TurfList.Values)
            {
                if (turf.GangId != Group.Id)
                    continue;

                turf.UpdateTurf(1000);
            }

            PolarEnvironment.GetGame().GetGroupManager().DeleteGroup(Group.Id);
            Session.SendNotification("You have successfully deleted your gang!");
            return;
        }
    }
}
