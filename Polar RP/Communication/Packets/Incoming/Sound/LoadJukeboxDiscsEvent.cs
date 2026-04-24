using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.Communication.Packets.Outgoing.Sound;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Incoming.Sound
{
    class LoadJukeboxDiscsEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            if (Session.GetHabbo().CurrentRoom != null)
                Session.SendMessage(new LoadJukeboxUserMusicItemsComposer(Session.GetHabbo().CurrentRoom));
        }
    }
}
