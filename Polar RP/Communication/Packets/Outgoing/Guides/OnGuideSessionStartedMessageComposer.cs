using System;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Guides
{
    internal class OnGuideSessionStartedMessageComposer : ServerPacket
    {
        public GameClient Session { get; }
        public GameClient requester { get; }
        public OnGuideSessionStartedMessageComposer(GameClient Session, GameClient requester)
            : base(ServerPacketHeader.OnGuideSessionStartedComposer)
        {
            this.Session = Session;
            this.requester = requester;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(requester.GetHabbo().Id);
            packet.WriteString(requester.GetHabbo().Username);
            packet.WriteString(requester.GetHabbo().Look);
            packet.WriteInteger(Session.GetHabbo().Id);
            packet.WriteString(Session.GetHabbo().Username);
            packet.WriteString(Session.GetHabbo().Look);
        }
    }
}