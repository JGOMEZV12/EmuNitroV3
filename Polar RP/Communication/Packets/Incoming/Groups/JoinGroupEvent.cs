using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Users.Messenger;
using Polar.Communication.Packets.Outgoing.Messenger;
using Polar.HabboHotel.Groups;
using Polar.Communication.Packets.Outgoing.Groups;
using Polar.Communication.Packets.Outgoing.Moderation;
using Polar.HabboHotel.Cache;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.Communication.Packets.Incoming.Groups
{
    internal class JoinGroupEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            if (Session == null || Session.GetHabbo() == null)
                return;

            int GroupId = Packet.PopInt();
            Group Group = null;
            GroupRank GroupRank = null;

            if (GroupId < 1000)
            {
                Group = GroupManager.GetJob(GroupId);
                GroupRank = GroupManager.GetJobRank(GroupId, 1);
            }
            else if (GroupManager.GetGang(GroupId).IsGang)
            {
                Group = GroupManager.GetGang(GroupId);
                GroupRank = GroupManager.GetGangRank(GroupId, 1);
            }

            if (GroupId >= 1000)
            {
                Group CurrentGang = GroupManager.GetGang(Session.GetRoleplay().GangId);

                if (CurrentGang != null && CurrentGang.CreatorId == Session.GetHabbo().Id)
                {
                    Session.SendNotification("¡Debes borrar tu pandilla primero si quieres unirte a otra pandilla!");
                    return;
                }
            }

            if (Group == null)
                return;

            if (Group.IsMember(Session.GetHabbo().Id) || Group.IsAdmin(Session.GetHabbo().Id))
                return;

            if (Group.GroupType == GroupType.LOCKED && Group.HasRequest(Session.GetHabbo().Id))
                return;

            if (Group.GroupType == GroupType.PRIVATE)
                return;

            List<Group> Groups = PolarEnvironment.GetGame().GetGroupManager().GetGroupsForUser(Session.GetHabbo().Id);

            if (Groups.Count >= 3)
            {
                Session.SendMessage(new BroadcastMessageAlertComposer("¡Vaya, parece que has alcanzado el límite de membresía de grupo!Sólo puedes unirte a 1.500 grupos."));
                return;
            }

            if (Group.Id < 1000 && GroupRank.HasCommand("guide"))
            {
                if (BlackListManager.BlackList.Contains(Session.GetHabbo().Id))
                {
                    Session.SendWhisper("Usted ha sido lista negra de unirse a la corporación de la policía!", 1);
                    return;
                }
            }

            if (Group.Id < 1000)
            {
                if (Group.GroupType == GroupType.OPEN && !Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                {
                    Session.GetRoleplay().JobRequest = GroupId;
                    Group.Requests.Add(Session.GetHabbo().Id);

                    Session.SendWhisper("Éxito, usted ha solicitado con éxito a unirse a la corporación '" + Group.Name + "'!", 1);
                    List<GameClient> GroupAdmins = (from Client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList() where Client != null && Client.GetHabbo() != null && (Group.IsAdmin(Client.GetHabbo().Id) || Client.GetHabbo().GetPermissions().HasRight("corporation_rights")) select Client).ToList();
                    foreach (GameClient Client in GroupAdmins)
                    {
                        Client.SendMessage(new GroupMembershipRequestedComposer(Group.Id, Session.GetHabbo(), 3));
                    }
                    Session.SendMessage(new GroupInfoComposer(Group, Session));
                    return;
                }
                else
                {
                    int Bubble = 4;
                    if (Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                        Bubble = 23;

                    Session.Shout("*Se contrata como " + Group.Name + " " + GroupRank.Name + "*", Bubble);

                    //Session.SendMessage(new GroupFurniConfigComposer(PolarEnvironment.GetGame().GetGroupManager().GetGroupsForUser(Session.GetHabbo().Id)));

                    Session.GetRoleplay().TimeWorked = 0;
                    Session.GetRoleplay().JobId = Group.Id;
                    Session.GetRoleplay().JobRank = 1;
                    Session.GetRoleplay().JobRequest = 0;

                    Group.AddNewMember(Session.GetHabbo().Id);
                    Group.SendPackets(Session);
                }
            }
            else
            {
                if (Group.GroupType == GroupType.LOCKED)
                {
                    Session.GetRoleplay().GangRequest = GroupId;
                    Group.Requests.Add(Session.GetHabbo().Id);

                    Session.SendWhisper("Éxito, has solicitado con éxito a unirse a la pandilla '" + Group.Name + "'!", 1);

                    List<GameClient> GroupAdmins = (from Client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList() where Client != null && Client.GetHabbo() != null && (Group.IsAdmin(Client.GetHabbo().Id)) select Client).ToList();
                    foreach (GameClient Client in GroupAdmins)
                    {
                        Client.SendMessage(new GroupMembershipRequestedComposer(Group.Id, Session.GetHabbo(), 3));
                    }

                    Session.SendMessage(new GroupInfoComposer(Group, Session));

                    UserCache Junk = null;
                    PolarEnvironment.GetGame().GetCacheManager().TryRemoveUser(Session.GetHabbo().Id, out Junk);
                    PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Session.GetHabbo().Id);
                }
                else
                {
                    Session.Shout("*Entra en '" + Group.Name + "' como " + GroupRank.Name + "*", 4);

                    //Session.SendMessage(new GroupFurniConfigComposer(PolarEnvironment.GetGame().GetGroupManager().GetGroupsForUser(Session.GetHabbo().Id)));

                    Session.GetRoleplay().GangId = Group.Id;
                    Session.GetRoleplay().GangRank = 1;
                    Session.GetRoleplay().GangRequest = 0;

                    Group.AddNewMember(Session.GetHabbo().Id);
                    Group.SendPackets(Session);
                    if (Group.HasChat)
                    {
                        var Clientx = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Session.GetHabbo().Id);
                        if (Clientx != null)
                        {
                            MessengerBuddy newgroup = new MessengerBuddy(int.MinValue + Group.Id, Group.Name, Group.Badge, string.Empty, 0, false, true, false);
                            Clientx.SendMessage(new FriendListUpdateComposer(Group, 0));
                        }
                    }
                } 
            }
        }
    }
}
