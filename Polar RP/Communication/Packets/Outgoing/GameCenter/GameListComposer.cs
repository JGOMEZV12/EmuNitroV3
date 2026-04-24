using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Games;

namespace Polar.Communication.Packets.Outgoing.GameCenter
{
    internal class GameListComposer : ServerPacket
    {
        public ICollection<GameData> Games { get; }

        public GameListComposer(ICollection<GameData> Games)
            : base(ServerPacketHeader.GameListMessageComposer)
        {
            this.Games = Games;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(PolarEnvironment.GetGame().GetGameDataManager().GetCount());//Game count
            foreach (GameData Game in Games)
            {
                packet.WriteInteger(Game.GameId);
                packet.WriteString(Game.GameName);
                packet.WriteString(Game.ColourOne);
                packet.WriteString(Game.ColourTwo);
                packet.WriteString(Game.ResourcePath);
                packet.WriteString(Game.StringThree);
            }
        }
    }
}
