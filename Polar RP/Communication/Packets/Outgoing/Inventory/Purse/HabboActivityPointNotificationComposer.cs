using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Inventory.Purse
{
    internal class HabboActivityPointNotificationComposer : ServerPacket
    {
        public int Balance { get; }
        public int Notify { get; }
        public int Type { get; }

        public HabboActivityPointNotificationComposer(int balance, int notify, int type = 0)
            : base(ServerPacketHeader.HabboActivityPointNotificationMessageComposer)
        {
            this.Balance = balance;
            this.Notify = notify;
            this.Type = type;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Balance);
            packet.WriteInteger(Notify);
            packet.WriteInteger(Type);
        }
    }
}
