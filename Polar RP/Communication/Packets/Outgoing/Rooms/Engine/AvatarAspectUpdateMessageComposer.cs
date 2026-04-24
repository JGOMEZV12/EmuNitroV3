
namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    internal class AvatarAspectUpdateComposer : ServerPacket
    {
        public AvatarAspectUpdateComposer(string Figure, string Gender)
            : base(ServerPacketHeader.AvatarAspectUpdateMessageComposer)
        {
            base.WriteString(Figure);
            base.WriteString(Gender);
        }

    }
}