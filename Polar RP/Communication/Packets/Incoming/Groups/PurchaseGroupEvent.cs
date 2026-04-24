using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Polar.HabboHotel.Users.Messenger;
using Polar.Communication.Packets.Outgoing.Messenger;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Groups;
using Polar.Communication.Packets.Outgoing.Groups;
using Polar.Communication.Packets.Outgoing.Catalog;
using Polar.Communication.Packets.Outgoing.Inventory.Purse;
using Polar.Communication.Packets.Outgoing.Moderation;
using Polar.Communication.Packets.Outgoing.Rooms.Session;

namespace Polar.Communication.Packets.Incoming.Groups
{
    internal class PurchaseGroupEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            if (Session.GetRoleplay().GangId > 1000)
            {
                Group Gang = GroupManager.GetGang(Session.GetRoleplay().GangId);

                if (Gang != null)
                {
                    if (Gang.CreatorId == Session.GetHabbo().Id)
                    {
                        Session.SendMessage(new BroadcastMessageAlertComposer("Por favor borre su pandilla actual antes de intentar hacer una nueva pandilla!"));
                        return;
                    }
                }
            }

            if (Session.GetHabbo().Credits < RoleplayManager.GangsPrice)
            {
                Session.SendMessage(new BroadcastMessageAlertComposer("A group costs " + RoleplayManager.GangsPrice + " credits! You only have " + Session.GetHabbo().Credits + "!"));
                return;
            }
            else
            {
                Session.GetHabbo().Credits -= RoleplayManager.GangsPrice;
                Session.SendMessage(new CreditBalanceComposer(Session.GetHabbo().Credits));
            }

            string word;
            string Name = Packet.PopString();
            Name = PolarEnvironment.GetGame().GetChatManager().GetFilter().IsUnnaceptableWord(Name, out word) ? "Spam" : Name;
            string Description = Packet.PopString();
            Description = PolarEnvironment.GetGame().GetChatManager().GetFilter().IsUnnaceptableWord(Description, out word) ? "Spam" : Description;
            int RoomId = Packet.PopInt();
            int Colour1 = Packet.PopInt();
            int Colour2 = Packet.PopInt();
            int groupID3 = Packet.PopInt();
            int groupID4 = Packet.PopInt();
            int groupID5 = Packet.PopInt();
            int groupID6 = Packet.PopInt();
            int groupID7 = Packet.PopInt();
            int groupID8 = Packet.PopInt();
            int groupID9 = Packet.PopInt();
            int groupID10 = Packet.PopInt();
            int groupID11 = Packet.PopInt();
            int groupID12 = Packet.PopInt();
            int groupID13 = Packet.PopInt();
            int groupID14 = Packet.PopInt();
            int groupID15 = Packet.PopInt();
            int groupID16 = Packet.PopInt();
            int groupID17 = Packet.PopInt();
            int groupID18 = Packet.PopInt();

            RoomData Room = PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(RoomId);
            if (Room == null || Room.OwnerId != Session.GetHabbo().Id || Room.Group != null)
                return;

            string Base = "b" + ((groupID4 < 10) ? "0" + groupID4.ToString() : groupID4.ToString()) + ((groupID5 < 10) ? "0" + groupID5.ToString() : groupID5.ToString()) + groupID6;
            string Symbol1 = "s" + ((groupID7 < 10) ? "0" + groupID7.ToString() : groupID7.ToString()) + ((groupID8 < 10) ? "0" + groupID8.ToString() : groupID8.ToString()) + groupID9;
            string Symbol2 = "s" + ((groupID10 < 10) ? "0" + groupID10.ToString() : groupID10.ToString()) + ((groupID11 < 10) ? "0" + groupID11.ToString() : groupID11.ToString()) + groupID12;
            string Symbol3 = "s" + ((groupID13 < 10) ? "0" + groupID13.ToString() : groupID13.ToString()) + ((groupID14 < 10) ? "0" + groupID14.ToString() : groupID14.ToString()) + groupID15;
            string Symbol4 = "s" + ((groupID16 < 10) ? "0" + groupID16.ToString() : groupID16.ToString()) + ((groupID17 < 10) ? "0" + groupID17.ToString() : groupID17.ToString()) + groupID18;

            Symbol1 = PolarEnvironment.GetGame().GetGroupManager().CheckActiveSymbol(Symbol1);
            Symbol2 = PolarEnvironment.GetGame().GetGroupManager().CheckActiveSymbol(Symbol2);
            Symbol3 = PolarEnvironment.GetGame().GetGroupManager().CheckActiveSymbol(Symbol3);
            Symbol4 = PolarEnvironment.GetGame().GetGroupManager().CheckActiveSymbol(Symbol4);

            string Badge = Base + Symbol1 + Symbol2 + Symbol3 + Symbol4;

            Group Group = null;
            if (!PolarEnvironment.GetGame().GetGroupManager().TryCreateGroup(Session.GetHabbo(), Name, Description, RoomId, Badge, Colour1.ToString(), Colour2.ToString(), out Group))
            {
                Session.SendNotification("An error occurred whilst trying to create this group.\n\nThis might've happened because you clicked purchase more than once!\n\nIn that case, your gang is created still.\n\n(Check by clicking your user and viewing the skull at the bottom right, or your profile).\n\nOtherwise you can try to create another gang!");
                return;
            }

            Session.SendMessage(new PurchaseOKComposer());

            Room.Group = Group;
            Room.Group.Id = Group.Id;

            if (Session.GetHabbo().CurrentRoomId != Room.Id)
                Session.SendMessage(new RoomForwardComposer(Room.Id));

            Session.SendMessage(new NewGroupInfoComposer(RoomId, Group.Id));

            if (Group.HasChat)
            {
                var Clientx = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Session.GetHabbo().Id);
                if (Clientx != null)
                {
                    MessengerBuddy newgroup = new MessengerBuddy(int.MinValue + Group.Id, Group.Name, Group.Badge, string.Empty, 0, false, true, false);
                    Session.SendMessage(new FriendListUpdateComposer(Group, 0));
                }
            }
        }
    }
}