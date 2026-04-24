using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Users;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Groups;
using Polar.Communication.Packets.Outgoing.Groups;
using Polar.Communication.Packets.Outgoing.Rooms.Permissions;
using Polar.HabboHotel.Cache;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Incoming.Groups
{
    internal class GiveAdminRightsEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            int GroupId = Packet.PopInt();
            int UserId = Packet.PopInt();

            if (GroupId >= 1000)
                return;

            Group Group = GroupManager.GetJob(GroupId);

            if (Group == null)
                return;

            if (!Session.GetHabbo().GetPermissions().HasRight("roleplay_corp_manager") && (Session.GetHabbo().Id != Group.CreatorId || !Group.IsMember(UserId)))
                return;

            Habbo Habbo = PolarEnvironment.GetHabboById(UserId);
            if (Habbo == null)
            {
                Session.SendNotification("Oops, an error occurred whilst finding this user.");
                return;
            }

            Group.MakeAdmin(UserId);
            GroupRank Rank = GroupManager.GetJobRank(Group.Id, 6);

            Session.Shout("*Promotes " + Habbo.Username + " all the way up to a " + Group.Name + " " + Rank.Name + "*", 23);

            #region (Disabled) Room Rights for Admin
            /*
            Room Room = null;
            if (PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(Group.RoomId, out Room))
            {
                RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(UserId);
                if (User != null)
                {
                    if (!User.Statusses.ContainsKey("flatctrl 3"))
                        User.AddStatus("flatctrl 3", "");

                    User.UpdateNeeded = true;
                    if (User.GetClient() != null)
                        User.GetClient().SendMessage(new YouAreControllerComposer(3));
                }
            }*/
            #endregion

            Session.SendMessage(new GroupMemberUpdatedComposer(GroupId, Habbo, 1));
        }
    }
}