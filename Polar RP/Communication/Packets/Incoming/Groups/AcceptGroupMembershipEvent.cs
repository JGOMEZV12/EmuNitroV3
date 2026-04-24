using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.Communication.Packets.Outgoing.Messenger;
using Polar.HabboHotel.Users.Messenger;
using Polar.HabboHotel.Groups;
using Polar.Communication.Packets.Outgoing.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Incoming.Groups
{
    internal class AcceptGroupMembershipEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            int groupId = packet.PopInt();
            int userId = packet.PopInt();
            Group group = null;
            GroupRank groupRank = null;

            if (groupId < 1000)
            {
                group = GroupManager.GetJob(groupId);
                groupRank = GroupManager.GetJobRank(groupId, 1);
            }
            else
            {
                group = GroupManager.GetGang(groupId);
                groupRank = GroupManager.GetGangRank(groupId, 1);
            }

            if (group == null)
                return;

            if (groupId < 1000 && !group.IsMember(session.GetHabbo().Id) && !session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                return;

            bool IsAdmin = false;
            if (group.IsAdmin(session.GetHabbo().Id))
                IsAdmin = true;
            if (groupId < 1000 && session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                IsAdmin = true;

            bool IsOwner = false;
            if (group.CreatorId == session.GetHabbo().Id)
                IsOwner = true;
            if (groupId < 1000 && session.GetHabbo().GetPermissions().HasRight("roleplay_corp_manager"))
                IsOwner = true;

            if (!IsAdmin && !IsOwner)
                return;

            if (!group.HasRequest(userId))
                return;

            Habbo Habbo = PolarEnvironment.GetHabboById(userId);

            if (Habbo == null)
            {
                session.SendNotification("¡Vaya!Se ha producido un error al encontrar este usuario.");
                return;
            }

            if (group.Id < 1000)
            {
                if (groupRank.HasCommand("guide"))
                {
                    if (BlackListManager.BlackList.Contains(userId))
                    {
                        group.HandleRequest(userId, false);
                        session.SendMessage(new GroupInfoComposer(group, session));
                        session.SendMessage(new UnknownGroupComposer(group.Id, userId));
                        session.SendWhisper("Lo sentimos, pero este usuario ha sido puesto en la lista negra de unirse a la corporación de la policía!", 1);
                        return;
                    }
                }
            }

            GameClient Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Habbo.Id);

            if (Client != null && Client.GetRoleplay() != null)
            {
                if (group.Id < 1000)
                {
                    if (Client.GetRoleplay().IsWorking)
                    {
                        Client.GetRoleplay().IsWorking = false;
                        WorkManager.RemoveWorkerFromList(Client);
                    }
                    Client.GetRoleplay().TimeWorked = 0;
                    Client.GetRoleplay().JobId = group.Id;
                    Client.GetRoleplay().JobRank = 1;
                    Client.GetRoleplay().JobRequest = 0;
                }
                else
                {
                    Client.GetRoleplay().GangId = group.Id;
                    Client.GetRoleplay().GangRank = 1;
                    Client.GetRoleplay().GangRequest = 0;
                }
            }

            group.HandleRequest(userId, true);
            group.SendPackets(Client);
            if (group.HasChat)
            {
                var Clientx = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(userId);
                if (Clientx != null)
                {
                    MessengerBuddy newgroup = new MessengerBuddy(int.MinValue + group.Id, group.Name, group.Badge, string.Empty, 0, false, true, false);
                    Clientx.SendMessage(new FriendListUpdateComposer(group, 1));
                }
            }

            string Username = Habbo == null ? "someone" : Habbo.Username;

            if (group.Id < 1000)
                session.SendWhisper("Excelente, aceptaste a " + Username + " en el '" + group.Name + "' corporación como '" + groupRank.Name + "'!", 1);
            else
                session.SendWhisper("Excelente, has aceptado " + Username + " en el '" + group.Name + "' de las mafías como '" + groupRank.Name + "'!", 1);
            session.SendMessage(new GroupMemberUpdatedComposer(group.Id, Habbo, 4));
        }
    }
}