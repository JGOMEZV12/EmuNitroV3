using Polar.HabboHotel.Rooms;
using Polar.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    internal class UserUpdateComposer : ServerPacket
    {
        public ICollection<RoomUser> RoomUsers { get; }

        public UserUpdateComposer(ICollection<RoomUser> users)
            : base(ServerPacketHeader.UserUpdateMessageComposer)
        {
            this.RoomUsers = users;
            Compose(this);
        }
        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(RoomUsers.Count);
            foreach (RoomUser User in RoomUsers.ToList())
            {
                packet.WriteInteger(User.VirtualId);
                packet.WriteInteger(User.X);
                packet.WriteInteger(User.Y);
                packet.WriteString(TextHandling.GetString(User.Z));
                packet.WriteInteger(User.RotHead);
                packet.WriteInteger(User.RotBody);

                StringBuilder StatusComposer = new StringBuilder();
                StatusComposer.Append("/");

                foreach (KeyValuePair<string, string> Status in User.Statusses.ToList())
                {
                    StatusComposer.Append(Status.Key);

                    if (!String.IsNullOrEmpty(Status.Value))
                    {
                        StatusComposer.Append(" ");
                        StatusComposer.Append(Status.Value);
                    }

                    StatusComposer.Append("/");
                }

                StatusComposer.Append("/");
                packet.WriteString(StatusComposer.ToString());
            }
        }
    }
}