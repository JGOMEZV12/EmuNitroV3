using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Utilities;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.Communication.Packets.Outgoing.Moderation
{
    internal class ModeratorUserRoomVisitsComposer : ServerPacket
    {
        public Habbo Habbo { get; }
        public Dictionary<double, RoomData> Visits { get; }

        public ModeratorUserRoomVisitsComposer(Habbo Data, Dictionary<double, RoomData> Visits)
            : base(ServerPacketHeader.ModeratorUserRoomVisitsMessageComposer)
        {
            this.Habbo = Data;
            this.Visits = Visits;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Habbo.Id);
            packet.WriteString(Habbo.Username);
            packet.WriteInteger(Visits.Count);

            foreach (KeyValuePair<double, RoomData> Visit in Visits)
            {
                packet.WriteInteger(Visit.Value.Id);
                packet.WriteString(Visit.Value.Name);
                packet.WriteInteger(UnixTimestamp.FromUnixTimestamp(Visit.Key).Hour);
                packet.WriteInteger(UnixTimestamp.FromUnixTimestamp(Visit.Key).Minute);
            }
        }
    }
}

