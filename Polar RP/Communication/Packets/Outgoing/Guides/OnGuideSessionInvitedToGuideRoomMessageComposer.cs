using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;

namespace Polar.Communication.Packets.Outgoing.Guides
{
    internal class OnGuideSessionInvitedToGuideRoomComposer : ServerPacket
    {
        public GameClient Session { get; }
        public OnGuideSessionInvitedToGuideRoomComposer(GameClient Session)
            : base(ServerPacketHeader.OnGuideSessionInvitedToGuideRoomComposer)
        {
            this.Session = Session;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            Room room = Session.GetHabbo().CurrentRoom;

            if (room == null)
            {
                packet.WriteInteger(0);
                packet.WriteString(string.Empty);
            }
            else
            {
                packet.WriteInteger(room.RoomId);
                packet.WriteString(room.RoomData.Name);
            }
        }
    }
}