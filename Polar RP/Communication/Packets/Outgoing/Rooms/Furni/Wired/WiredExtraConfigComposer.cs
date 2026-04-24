using Polar.HabboHotel.Items.Wired;

namespace Polar.Communication.Packets.Outgoing.Rooms.Furni.Wired
{
    class WiredExtraConfigComposer : ServerPacket
    {
        public WiredExtraConfigComposer(IWiredItem Box)
            : base(ServerPacketHeader.WiredEffectConfigMessageComposer)
        {
            Box.Serialize(this);
            base.WriteInteger(0);
        }
    }
}
