using System;
using System.Linq;
using System.Text;

using Polar.Communication.Packets.Outgoing.Inventory.Weapons;

namespace Polar.Communication.Packets.Incoming.Inventory.Weapons
{
    internal class GetWeaponsEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            Session.SendMessage(new WeaponsComposer(Session));
            //Console.WriteLine("Aqui pendejo");
        }
    }
}
