using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Inventory.Purse
{
    internal class CreditBalanceComposer : ServerPacket
    {
        public int CreditsBalance { get; }

        public CreditBalanceComposer(int creditsBalance)
            : base(ServerPacketHeader.CreditBalanceMessageComposer)
        {
            this.CreditsBalance = creditsBalance;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteString(CreditsBalance + ".0");
        }
    }
}