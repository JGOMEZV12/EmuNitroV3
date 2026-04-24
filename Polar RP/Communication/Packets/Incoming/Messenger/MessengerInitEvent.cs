using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using MoreLinq;
using Polar.HabboHotel.Users.Messenger;
using Polar.Communication.Packets.Outgoing.Messenger;

namespace Polar.Communication.Packets.Incoming.Messenger
{
    internal class MessengerInitEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            if (Session == null || Session.GetHabbo() == null || Session.GetHabbo().GetMessenger() == null)
                return;

            Session.GetHabbo().GetMessenger().OnStatusChanged(false);

            ICollection<MessengerBuddy> Friends = new List<MessengerBuddy>();
            foreach (MessengerBuddy Buddy in Session.GetHabbo().GetMessenger().GetFriends().ToList())
            {
                if (Buddy == null || Buddy.IsOnline || Buddy.isBot)
                    continue;

                Friends.Add(Buddy);
            }

            Session.SendMessage(new MessengerInitComposer());

            Session.SendMessage(new BuddyListComposer(Friends, Session.GetHabbo(), 1, 0));
            //Session.SendMessage(new BuddyListComposer(Friends, Session.GetHabbo()));

            Session.GetRoleplay().LoadBotFriendships();

            Session.GetHabbo().GetMessenger().ProcessOfflineMessages();
        }
    }
}