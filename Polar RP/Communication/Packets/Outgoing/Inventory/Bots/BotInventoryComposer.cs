using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Users.Inventory.Bots;

namespace Polar.Communication.Packets.Outgoing.Inventory.Bots
{
    internal class BotInventoryComposer : ServerPacket
    {
        public ICollection<Bot> Bots { get; }

        public BotInventoryComposer(ICollection<Bot> Bots)
            : base(ServerPacketHeader.BotInventoryMessageComposer)
        {
            this.Bots = Bots;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Bots.Count);
            foreach (Bot Bot in Bots.ToList())
            {
                packet.WriteInteger(Bot.Id);
                packet.WriteString(Bot.Name);
                packet.WriteString(Bot.Motto);
                packet.WriteString(Bot.Gender);
                packet.WriteString(Bot.Figure);
            }
        }
    }
}