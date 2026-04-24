using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;

namespace Polar.Communication.Packets.Outgoing.Moderation
{
    internal class ModeratorRoomInfoComposer : ServerPacket
    {
        public RoomData Data { get; }
        public bool OwnerInRoom { get; }

        public ModeratorRoomInfoComposer(RoomData Data, bool OwnerInRoom)
            : base(ServerPacketHeader.ModeratorRoomInfoMessageComposer)
        {
            this.Data = Data;
            this.OwnerInRoom = OwnerInRoom;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Data.Id);
            packet.WriteInteger(Data.UsersNow);
            packet.WriteBoolean(OwnerInRoom); // owner in room
            packet.WriteInteger(Data.OwnerId);
            packet.WriteString(Data.OwnerName);
            packet.WriteBoolean(Data != null);
            packet.WriteString(Data.Name);
            packet.WriteString(Data.Description);

            packet.WriteInteger(Data.Tags.Count);
            foreach (string Tag in Data.Tags)
            {
                packet.WriteString(Tag);
            }

            packet.WriteBoolean(false);
        }
    }
}
