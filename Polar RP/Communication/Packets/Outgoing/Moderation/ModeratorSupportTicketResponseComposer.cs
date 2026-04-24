using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Moderation
{
    internal class ModeratorSupportTicketResponseComposer : ServerPacket
    {
        public int Result { get; }
        public string message { get; }
        public ModeratorSupportTicketResponseComposer(int Result)
            : base(ServerPacketHeader.ModeratorSupportTicketResponseMessageComposer)
        {
            this.Result = Result;
            Compose(this);

        }

        public ModeratorSupportTicketResponseComposer(int Result, string message)
            : base(ServerPacketHeader.ModeratorSupportTicketResponseMessageComposer)
        {
            this.Result = Result;
            this.message = message;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            if (message == ""){
                packet.WriteInteger(Result);
                packet.WriteString(message);
            }
            else
            {
                packet.WriteInteger(Result);
                packet.WriteString("");
            }
           
        }
    }
}