using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;

namespace Polar.Communication.Packets.Outgoing.Groups
{
    internal class GroupCreationWindowComposer : ServerPacket
    {
        public ICollection<RoomData> Rooms { get; }

        public GroupCreationWindowComposer(ICollection<RoomData> rooms)
            : base(ServerPacketHeader.GroupCreationWindowMessageComposer)
        {
            this.Rooms = rooms;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(PolarStaticGameSettings.GroupPurchaseAmount);//Price

            packet.WriteInteger(Rooms.Count);//Room count that the user has.
            foreach (RoomData Room in Rooms)
            {
                packet.WriteInteger(Room.Id);//Room Id
                packet.WriteString(Room.Name);//Room Name
                packet.WriteBoolean(false);//What?
            }

            packet.WriteInteger(5);
            packet.WriteInteger(5);
            packet.WriteInteger(11);
            packet.WriteInteger(4);

            packet.WriteInteger(6);
            packet.WriteInteger(11);
            packet.WriteInteger(4);

            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(0);

            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(0);

            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }
    }
}
