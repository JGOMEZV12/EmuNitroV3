using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Groups;
using Polar.Communication.Packets.Outgoing.Groups;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;

using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Cache;

namespace Polar.Communication.Packets.Incoming.Groups
{
    internal class DeclineGroupMembershipEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            int GroupId = Packet.PopInt();
            int UserId = Packet.PopInt();
            Group Group = null;
            GroupRank GroupRank = null;

            if (GroupId < 1000)
            {
                Group = GroupManager.GetJob(GroupId);
                GroupRank = GroupManager.GetJobRank(GroupId, 1);
            }
            else
            {
                Group = GroupManager.GetGang(GroupId);
                GroupRank = GroupManager.GetGangRank(GroupId, 1);
            }

            if (Group == null)
                return;

            bool IsAdmin = false;
            if (Group.IsAdmin(Session.GetHabbo().Id))
                IsAdmin = true;
            if (GroupId < 1000 && Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                IsAdmin = true;

            bool IsOwner = false;
            if (Group.CreatorId == Session.GetHabbo().Id)
                IsOwner = true;
            if (GroupId < 1000 && Session.GetHabbo().GetPermissions().HasRight("roleplay_corp_manager"))
                IsOwner = true;

            if (!IsAdmin && !IsOwner)
                return;

            if (!Group.HasRequest(UserId))
                return;

            Habbo Habbo = PolarEnvironment.GetHabboById(UserId);

            if (Habbo == null)
            {
                Session.SendNotification("Oops, hubo un error no se pudo encontrar este usuario.");
                return;
            }

            Group.HandleRequest(UserId, false);

            Habbo = PolarEnvironment.GetHabboById(UserId);

            if (Habbo != null)
            {
                if (Group.Id < 1000)
                    Session.SendWhisper("Success, Tu despides a " + Habbo.Username + " de la empresa '" + Group.Name + "' !", 1);
                else
                    Session.SendWhisper("Success, tu despides '" + Habbo.Username + "' de la pandilla '" + Group.Name + "' !", 1);
            }
            else
            {
                using (UserCache Member = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(UserId))
                {

                    if (Group.Id < 1000)
                        Session.SendWhisper("Success, Despides a " + Member.Username + " de la empresa '" + Group.Name + "' !", 1);
                    else
                        Session.SendWhisper("Success, Despides a '" + Member.Username + "' de la pandilla '" + Group.Name + "'!", 1);
                }
            }

            Session.SendMessage(new GroupInfoComposer(Group, Session));
            Session.SendMessage(new UnknownGroupComposer(Group.Id, UserId));

            if (Group.Id < 1000 && Habbo.GetClient() != null && Habbo.GetClient().GetRoomUser() != null)
            {
                if (Habbo.CurrentRoom != null && Habbo.CurrentRoom.TutorialEnabled)
                    Habbo.SendComposerToCorrectUsers(new UsersComposer(Habbo.GetClient().GetRoomUser()));
            }
        }
    }
}