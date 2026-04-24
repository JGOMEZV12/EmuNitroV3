using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Users
{
    internal class UpdateUsernameComposer : ServerPacket
    {
        public string Username { get; }
        public UpdateUsernameComposer(string username)
            : base(ServerPacketHeader.UpdateUsernameMessageComposer)
        {
            this.Username = username;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(0);
            packet.WriteString(Username);
            packet.WriteInteger(0);
        }
    }
}