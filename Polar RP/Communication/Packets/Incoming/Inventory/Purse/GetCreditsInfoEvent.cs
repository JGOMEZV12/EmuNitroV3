using System;
using System.Linq;
using System.Text;

using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Inventory.Purse;

namespace Polar.Communication.Packets.Incoming.Inventory.Purse
{
    internal class GetCreditsInfoEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            Session.SendMessage(new CreditBalanceComposer(Session.GetHabbo().Credits));
            Session.SendMessage(new ActivityPointsComposer(Session.GetHabbo().Duckets, Session.GetHabbo().Diamonds, Session.GetHabbo().EventPoints));
        }
    }
}
