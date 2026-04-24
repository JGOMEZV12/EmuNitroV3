using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Users.Messenger;

namespace Polar.Communication.Packets.Outgoing.Messenger
{
    internal class InstantMessageErrorComposer : ServerPacket
    {
        public MessengerMessageErrors Error { get; }
        public int Target { get; }

        public InstantMessageErrorComposer(MessengerMessageErrors Error, int Target)
            : base(ServerPacketHeader.InstantMessageErrorMessageComposer)
        {
            this.Error = Error;
            this.Target = Target;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(MessengerMessageErrorsUtility.GetMessageErrorPacketNum(Error));
            packet.WriteInteger(Target);
            packet.WriteString("");
        }
    }
}
