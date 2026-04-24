using System;

using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Guides;
using Polar.Communication.Packets.Outgoing.Guides;
using Polar.Utilities;
using Polar.HabboHotel.Rooms.Chat.Commands;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Incoming.Guides
{
    internal class OnGuideMessageEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            int UserId = Packet.PopInt();
            string Message = Packet.PopString();

            GuideManager guideManager = PolarEnvironment.GetGame().GetGuideManager();
            List<GameClient> HandlingCalls = guideManager.HandlingCalls();

            if (HandlingCalls.Count < 1)
            {
                Session.SendMessage(new OnGuideSessionError());
                return;
            }

            CryptoRandom Random = new CryptoRandom();
            GameClient RandomPolice = null;

            if (HandlingCalls.Count > 1)
                RandomPolice = HandlingCalls[Random.Next(0, HandlingCalls.Count)];
            else
                RandomPolice = HandlingCalls[0];

            if (RandomPolice == null)
            {
                Session.SendMessage(new OnGuideSessionError());
                return;
            }

            Session.SendMessage(new OnGuideSessionAttachedComposer(Session.GetHabbo().Id, Message, 30));
            RandomPolice.SendMessage(new OnGuideSessionAttachedComposer(Session.GetHabbo().Id, Message, 15));

            Session.GetRoleplay().SentRealCall = true;
            Session.GetRoleplay().Sent911Call = true;
            Session.GetRoleplay().CallMessage = Message;
            Session.GetRoleplay().GuideOtherUser = RandomPolice;
            RandomPolice.GetRoleplay().GuideOtherUser = Session;
        }
    }
}
