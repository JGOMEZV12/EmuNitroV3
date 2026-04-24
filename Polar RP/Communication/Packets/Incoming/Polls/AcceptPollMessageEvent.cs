using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Polls;
using Polar.Communication.Packets.Outgoing.Polls;

namespace Polar.Communication.Packets.Incoming.Polls
{
    internal class AcceptPollMessageEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            int key = Packet.PopInt();
            Poll poll = null;

            if (key == 500000)
            {
                string PollName = "HoloRP ATM";
                string PollInvitation = "HoloRP ATM";
                string PollThanks = "Thanks for using HoloRP's ATM!";

                poll = new Poll(500000, 0, PollName, PollInvitation, PollThanks, "", 1, null);
            }
            else
                poll = PolarEnvironment.GetGame().GetPollManager().Polls[key];

            Session.SendMessage(new PollQuestionsMessageComposer(Session, poll));
        }
    }
}
