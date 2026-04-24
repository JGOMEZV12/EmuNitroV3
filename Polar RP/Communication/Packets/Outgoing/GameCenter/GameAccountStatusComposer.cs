using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.GameCenter
{
    internal class GameAccountStatusComposer : ServerPacket
    {
        public int GameId { get; }

        public GameAccountStatusComposer(int GameID)
            : base(ServerPacketHeader.GameAccountStatusMessageComposer)
        {
            this.GameId = GameID;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(GameId);
            packet.WriteInteger(-1); // Games Left
            packet.WriteInteger(0);//Was 16?
        }
    }
}