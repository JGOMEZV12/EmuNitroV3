using Polar.HabboHotel.Items.Wired;
namespace Polar.Communication.Packets.Outgoing.Rooms.Furni.Wired
{
    class WiredConditionConfigComposer : ServerPacket
    {
        public WiredConditionConfigComposer(IWiredItem Box)
            : base(ServerPacketHeader.WiredConditionConfigMessageComposer)
        {
            Box.Serialize(this);
            WriteInteger(0); // Conflicts
        }
    }
}