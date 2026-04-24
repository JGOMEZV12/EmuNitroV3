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
    internal class TakeAdminRightsEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            int GroupId = Packet.PopInt();
            int UserId = Packet.PopInt();

            if (GroupManager.GetGang(GroupId).IsGang == true)
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

            Group.TakeAdmin(UserId);
            GroupRank Rank = GroupManager.GetJobRank(Group.Id, 1);

            Session.Shout("*Demotes " + Habbo.Username + " all the way down to a " + Group.Name + " " + Rank.Name + "*", 23);

            #region (Disabled) Take Room Rights
            /*
            Room Room = null;
            if (PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(Group.RoomId, out Room))
            {
                RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(UserId);
                if (User != null)
                {
                    if (User.Statusses.ContainsKey("flatctrl 3"))
                        User.RemoveStatus("flatctrl 3");
                    User.UpdateNeeded = true;
                    if (User.GetClient() != null)
                        User.GetClient().SendMessage(new YouAreControllerComposer(0));
                }
            }*/
            #endregion

            Session.SendMessage(new GroupMemberUpdatedComposer(GroupId, Habbo, 2));
        }
    }
}
