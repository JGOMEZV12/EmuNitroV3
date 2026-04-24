using System;

using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Guides;
using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Guides;

namespace Polar.Communication.Packets.Incoming.Guides
{
    internal class GuideEndSession : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            if (Session == null || Session.GetRoleplay() == null)
                return;

            GameClient requester = Session.GetRoleplay().GuideOtherUser;

            if (Session.GetRoleplay().IsWorking)
            {
                if (requester != null)
                {
                    requester.SendMessage(new GuideSessionEndedMessageComposer(2));

                    if (requester.GetRoleplay() != null && !requester.GetRoleplay().SentRealCall)
                    {
                        requester.GetRoleplay().Sent911Call = false;
                        requester.GetRoleplay().GuideOtherUser = null;
                    }
                }

                Session.SendMessage(new GuideSessionEndedMessageComposer(2));
            }
            else
            {
                Session.SendMessage(new GuideSessionEndedMessageComposer(2));
                Session.GetRoleplay().Sent911Call = false;
                Session.GetRoleplay().GuideOtherUser = null;

                if (requester != null)
                    requester.SendMessage(new GuideSessionEndedMessageComposer(0));
            }
        }
    }
}
