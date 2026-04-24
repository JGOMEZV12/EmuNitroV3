using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Groups;
using Polar.Communication.Packets.Outgoing.Groups;
using Polar.Communication.Packets.Outgoing.Rooms.Permissions;

using Polar.Database.Interfaces;


namespace Polar.Communication.Packets.Incoming.Groups
{
    internal class UpdateGroupSettingsEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            int GroupId = Packet.PopInt();
            Group Group = null;


            if (GroupId < 1000)
                Group = GroupManager.GetJob(GroupId);
            else
                Group = GroupManager.GetGang(GroupId);

            if (Group == null)
                return;

            if (Group.CreatorId != Session.GetHabbo().Id && !Session.GetHabbo().GetPermissions().HasRight("roleplay_corp_manager"))
                return;

            int Type = Packet.PopInt();
            int FurniOptions = Packet.PopInt();

            switch (Type)
            {
                default:
                case 0:
                    Group.GroupType = GroupType.OPEN;
                    break;
                case 1:
                    Group.GroupType = GroupType.LOCKED;
                    break;
                case 2:
                    Group.GroupType = GroupType.PRIVATE;
                    break;
            }

            if (Group.GroupType != GroupType.LOCKED)
            {
                if (Group.Requests.Count > 0)
                {
                    foreach (int UserId in Group.Requests)
                    {
                        Group.HandleRequest(UserId, false);
                    }

                    Group.ClearRequests();
                }
            }

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                if (GroupId < 1000)
                    dbClient.SetQuery("UPDATE `rp_jobs` SET `state` = @GroupState, `admindeco` = @AdminDeco WHERE `id` = '" + Group.Id + "'");
                else
                    dbClient.SetQuery("UPDATE `rp_gangs` SET `state` = @GroupState, `admindeco` = @AdminDeco WHERE `id` = '" + Group.Id + "'");
                dbClient.AddParameter("GroupState", (Group.GroupType == GroupType.OPEN ? 0 : Group.GroupType == GroupType.LOCKED ? 1 : 2).ToString());
                dbClient.AddParameter("AdminDeco", (FurniOptions == 1 ? 1 : 0).ToString());
                dbClient.RunQuery();
            }

            Group.AdminOnlyDeco = FurniOptions;

            Room Room;
            if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(Group.RoomId, out Room))
                return;

            foreach (RoomUser User in Room.GetRoomUserManager().GetRoomUsers().ToList())
            {
                if (Room.OwnerId == User.UserId || Group.IsAdmin(User.UserId) || !Group.IsMember(User.UserId))
                    continue;

                if (FurniOptions == 1)
                {
                    User.SetStatus("flatctrl", "0");
                    User.UpdateNeeded = true;

                    User.GetClient().SendMessage(new YouAreControllerComposer(0));
                }
                else if (FurniOptions == 0 && !User.Statusses.ContainsKey("flatctrl"))
                {
                    User.SetStatus("flatctrl", "2");
                    User.UpdateNeeded = true;

                    User.GetClient().SendMessage(new YouAreControllerComposer(1));
                }
            }

            Session.SendMessage(new GroupInfoComposer(Group, Session));
        }
    }
}