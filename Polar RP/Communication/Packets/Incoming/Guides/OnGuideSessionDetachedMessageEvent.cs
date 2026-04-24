using System;
using System.Collections.Generic;
using Polar.Utilities;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Guides;
using Polar.Communication.Packets.Outgoing.Guides;

namespace Polar.Communication.Packets.Incoming.Guides
{
    internal class OnGuideSessionDetachedMessageEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            var state = Packet.PopBoolean();
            var requester = Session.GetRoleplay().GuideOtherUser;

            if (requester == null || requester.GetHabbo() == null || requester.GetRoleplay() == null)
            {
                Session.GetRoleplay().GuideOtherUser = null;
                Session.SendMessage(new OnGuideSessionDetachedComposer(0));
                Session.SendMessage(new OnGuideSessionDetachedComposer(1));
                return;
            }

            if (!state)
            {
                Session.GetRoleplay().GuideOtherUser = null;
                Session.SendMessage(new OnGuideSessionDetachedComposer(0));
                Session.SendMessage(new OnGuideSessionDetachedComposer(1));

                requester.SendWhisper("The police officer declined your call for help!", 1);

                GuideManager guideManager = PolarEnvironment.GetGame().GetGuideManager();
                List<GameClient> Handlers = null;

                Handlers = guideManager.HandlingCalls();

                if (Handlers.Count < 2)
                {
                    requester.SendMessage(new OnGuideSessionError());

                    requester.GetRoleplay().Sent911Call = false;
                    requester.GetRoleplay().CallMessage = "";
                    requester.GetRoleplay().GuideOtherUser = null;
                    return;
                }

                GameClient RandomPolice = Handlers[new CryptoRandom().Next(0, Handlers.Count)];

                while (RandomPolice == Session)
                {
                    RandomPolice = Handlers[new CryptoRandom().Next(0, Handlers.Count)];
                }

                if (RandomPolice == null)
                {
                    requester.SendMessage(new OnGuideSessionError());

                    requester.GetRoleplay().Sent911Call = false;
                    requester.GetRoleplay().CallMessage = "";
                    requester.GetRoleplay().GuideOtherUser = null;
                    return;
                }

                requester.SendWhisper("Your new call for help has been sent!", 1);
                requester.GetRoleplay().Sent911Call = true;
                requester.GetRoleplay().GuideOtherUser = RandomPolice;

                RandomPolice.SendMessage(new OnGuideSessionAttachedComposer(requester.GetHabbo().Id, requester.GetRoleplay().CallMessage, 15));
                return;
            }

            requester.SendWhisper(Session.GetHabbo().Username + " has opened your 911 call!", 1);
            requester.SendMessage(new OnGuideSessionStartedMessageComposer(Session, requester));
            Session.SendMessage(new OnGuideSessionStartedMessageComposer(Session, requester));
            Session.SendMessage(new OnGuideSessionMsgComposer(Session, "Click on 'Visit' above to help them!"));

            requester.GetRoleplay().CallMessage = "";
            requester.GetRoleplay().Sent911Call = false;
        }
    }
}
