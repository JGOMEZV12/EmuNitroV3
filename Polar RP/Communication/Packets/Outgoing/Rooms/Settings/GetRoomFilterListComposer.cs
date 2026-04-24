using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;

namespace Polar.Communication.Packets.Outgoing.Rooms.Settings
{
    internal class GetRoomFilterListComposer : ServerPacket
    {
        public Room Instance { get; }
        public GetRoomFilterListComposer(Room Instance)
            : base(ServerPacketHeader.GetRoomFilterListMessageComposer)
        {
            this.Instance = Instance;
            Compose(this);
        }
        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Instance.WordFilterList.Count);
            foreach (string Word in Instance.WordFilterList)
            {
               packet.WriteString(Word);
            }
        }
    }
}
