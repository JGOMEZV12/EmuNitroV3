using System;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Guides
{
    internal class OnGuideSessionMsgComposer : ServerPacket
    {
        public GameClient Session { get; }
        public string message { get; }
        public OnGuideSessionMsgComposer(GameClient Session, string message)
            : base(ServerPacketHeader.OnGuideSessionMsgComposer)
        {
            this.Session = Session;
            this.message = message;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteString(message);
            packet.WriteInteger(Session.GetHabbo().Id);
        }
    }
}