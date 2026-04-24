using Polar.HabboHotel.Rooms;

namespace Polar.Communication.Packets.Outgoing.Rooms.Avatar
{
    public class ActionComposer : ServerPacket
    {
        public int VirtualId { get; }
        public int Action { get; }

        public ActionComposer(int VirtualId, int Action)
            : base(ServerPacketHeader.ActionMessageComposer)
        {
            this.VirtualId = VirtualId;
            this.Action = Action;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(VirtualId);
            packet.WriteInteger(Action);
        }
    }
}