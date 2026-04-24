using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Polar.Communication.Packets.Outgoing.Rooms.Settings
{
    internal class RoomSettingsSavedComposer : ServerPacket
    {
        public int RoomId { get; }
        public RoomSettingsSavedComposer(int roomID)
            : base(ServerPacketHeader.RoomSettingsSavedMessageComposer)
        {
            this.RoomId = roomID;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(RoomId);
        }
    }
}