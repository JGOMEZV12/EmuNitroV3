using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.Database.Interfaces;

namespace Polar.Communication.Packets.Incoming.Polls
{
    internal class RefusePollMessageEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            int num = Packet.PopInt();

            if (num == 500000)
            {
                if (!Session.GetRoomUser().CanWalk)
                    Session.GetRoomUser().CanWalk = true;

                if (Session.GetRoleplay().ATMFailed)
                    Session.SendNotification("Error... The ATM Machine failed to respond to your request.");

                Session.GetRoleplay().ATMAccount = "";
                Session.GetRoleplay().ATMAction = "";
                Session.GetRoleplay().ATMFailed = false;
                return;
            }

            if (!Session.GetHabbo().AnsweredPolls.Contains(num))
            {
                Session.GetHabbo().AnsweredPolls.Add(num);
                Session.SendNotification("You have declined the poll! To be able to answer it later, please reload the RP!");
            }
        }
    }
}
