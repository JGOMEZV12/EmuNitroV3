using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.QuickPolls
{
    internal class QuickPollMessageComposer : ServerPacket
    {
        public string Question { get; }
        public int Time { get; }
        public QuickPollMessageComposer(string question, int time)
            : base(ServerPacketHeader.QuickPollMessageComposer)
        {
            this.Question = question;
            this.Time = time;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            Random rnd = new Random();
            packet.WriteString("");
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(Time);   //duration
            packet.WriteInteger(rnd.Next(1, 1000));  //id
            packet.WriteInteger(120); //number
            packet.WriteInteger(3);
            packet.WriteString(Question);
        }
    }
}