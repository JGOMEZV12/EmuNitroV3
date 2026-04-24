using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Users.Messenger;
using Polar.Communication.Packets.Outgoing.Messenger;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Users;
using Polar.Communication.Packets.Outgoing.Groups;
using Polar.Communication.Packets.Outgoing.Rooms.Permissions;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Cache;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Guides;

namespace Polar.Communication.Packets.Incoming.Groups
{
    internal class RemoveGroupMemberEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            int GroupId = Packet.PopInt();
            int UserId = Packet.PopInt();

            Group Group = null;
            //Group RemoveGroup = null;

            if (GroupId < 1000)
                Group = GroupManager.GetJob(GroupId);
            else
                Group = GroupManager.GetGang(GroupId);

            if (Group == null)
                return;

            if (GroupId == 1)
                return;

            /*if (GroupId == 1000)
                return;*/

            if (!Group.IsMember(UserId))
                return;

            UserCache Junk = null;
            PolarEnvironment.GetGame().GetCacheManager().TryRemoveUser(UserId, out Junk);
            PolarEnvironment.GetGame().GetCacheManager().GenerateUser(UserId);

            bool CanRemove = false;
            if (UserId == Session.GetHabbo().Id || Group.CreatorId == Session.GetHabbo().Id || Group.IsAdmin(Session.GetHabbo().Id) || Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                CanRemove = true;

            if (!CanRemove)
            {
                if (Group.Id < 1000)
                {
                    if (Group.IsAdmin(UserId) && Group.CreatorId != Session.GetHabbo().Id)
                    {
                        if (!Session.GetHabbo().GetPermissions().HasRight("roleplay_corp_manager"))
                        {
                            Session.SendWhisper("Lo sentimos, ¡solo el dueño de la corporación puede cambiar los gerentes de la corporación!", 1);
                            return;
                        }
                    }
                }
                return;
            }

            UserCache HabboTwo = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(UserId);
            Habbo Habbo = PolarEnvironment.GetHabboById(UserId);

            Group NewGroup;
            if (GroupId < 1000)
                NewGroup = GroupManager.GetJob(1);
            else
                NewGroup = GroupManager.GetGang(1000);

            #region (Disabled) Remove Room Rights
            /*
            if ((Group.AdminOnlyDeco == 0 || Group.IsAdmin(UserId)) && Client != null && Client.GetRoomUser() != null)
            {
                Room Room;
                if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(Group.RoomId, out Room))
                    return;

                RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Client.GetHabbo().Id);
                if (User != null)
                {
                    User.RemoveStatus("flatctrl 1");
                    User.UpdateNeeded = true;
                    Client.SendMessage(new YouAreControllerComposer(0));
                }
            }*/
            #endregion
            List<UserCache> Members = new List<UserCache>();
            if (Session.GetHabbo().Id == UserId)
            {
                int FinishIndex = 14 < Members.Count ? 14 : Members.Count;
                int MembersCount = Members.Count;
                Habbo UserRemove = PolarEnvironment.GetHabboById(UserId);
                if (UserRemove != null && UserRemove.GetClient() != null)
                {
                    UserRemove.GetClient().SendMessage(new FriendListUpdateComposer(-Group.Id));
                }

                UpdateGroupData(Group, Session);
                NewGroup.AddNewMember(UserId);
                NewGroup.SendPackets(Session);

                if (Group.Id < 1000)
                    Session.Shout("*Deja el trabajo " + Group.Name + " empresa*", 4);
                else
                    Session.Shout("*Abandona la " + Group.Name + " de pandillas*", 4);
            }
            else
            {
                GameClient Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(UserId);
                if (Client != null && Client.GetHabbo() != null && Client.GetRoleplay() != null)
                {
                    UpdateGroupData(Group, Client);
                    NewGroup.AddNewMember(UserId);
                    NewGroup.SendPackets(Client);
                    Group.SendMembersPackets(Client);
                    int FinishIndex = 14 < Members.Count ? 14 : Members.Count;
                    int MembersCount = Members.Count;
                   
                    if (Group.HasChat)
                    {
                        Habbo UserRemove = PolarEnvironment.GetHabboById(UserId);
                        if (UserRemove != null && UserRemove.GetClient() != null)
                        {
                            MessengerBuddy newgroup = new MessengerBuddy(int.MinValue + Group.Id, Group.Name, Group.Badge, string.Empty, 0, false, true, false);
                            UserRemove.GetClient().SendMessage(new FriendListUpdateComposer(Group, -1));
                        }
                    }
                }
                else
                {
                    NewGroup.AddNewMember(UserId);
                    Group.SendMembersPackets(Session);
                }

                int Bubble = Session.GetHabbo().GetPermissions().HasRight("mod_tool") ? 23 : 4;
                if (Group.CreatorId == Session.GetHabbo().Id)
                    Bubble = 4;

                string Username = HabboTwo == null ? "someone" : HabboTwo.Username;
                if (Group.Id < 1000)
                    Session.Shout("*Despedido " + Username + " de la  " + Group.Name + " empresa*", Bubble);
                else
                    Session.Shout("*Expulsado " + Username + " de la pandilla " + Group.Name + " pandilla o grupo criminal*", Bubble);
            }


            if (Group.Id >= 1000)
            { 
                foreach (int Member in Group.Members.Keys)
                {
                    GameClient GangMember = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Member);

                    if (GangMember == null)
                        continue;

                    GangMember.SendWhisper("[GANG] " + HabboTwo == null ? "someone" : HabboTwo.Username + " despedido de la pandilla!", 34);
                }
            }
        }

        public void UpdateGroupData(Group Group, GameClient Session)
        {
            if (Group == null || Session == null || Session.GetHabbo() == null || Session.GetRoleplay() == null)
                return;

            if (Group.Id < 1000)
            {
                if (Session.GetRoleplay().IsWorking)
                {
                    WorkManager.RemoveWorkerFromList(Session);
                    Session.GetRoleplay().IsWorking = false;
                    Session.GetHabbo().Poof();

                    if (GroupManager.HasJobCommand(Session, "guide"))
                    {
                        PolarEnvironment.GetGame().GetGuideManager().RemoveGuide(Session);
                        Session.SendMessage(new HelperToolConfigurationComposer(Session));
                    }
                }
                Session.GetRoleplay().TimeWorked = 0;
                Session.GetRoleplay().JobId = 1;
                Session.GetRoleplay().JobRank = 1;
                Session.GetRoleplay().JobRequest = 0;
            }
            else
            {
                Session.GetRoleplay().GangId = 0;
                Session.GetRoleplay().GangRank = 0;
                Session.GetRoleplay().GangRequest = 0;
            }
        }
    }
}