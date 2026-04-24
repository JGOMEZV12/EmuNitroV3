using System;
using System.Text;

namespace Polar.Messages.Net.MusCommunication
{
    public class MusPacketEvent
    {
        public string PacketName { get; set; }
        public string PacketData { get; set; }

        public MusPacketEvent()
        {

        }

        public MusPacketEvent(string PacketName, string PacketData)
        {
            this.PacketName = PacketName;
            this.PacketData = PacketData;
        }

        // "PacketName:Data_1|Data_2|...|Data_n"
        public MusPacketEvent(string Data)
        {
            int Split = Data.IndexOf(":", StringComparison.Ordinal);
            PacketName = Data.Substring(0, Split);
            PacketData = Data.Substring(PacketName.Length + 1);
        }

        public string Serialize()
        {
            return string.Format("{0}:{1}", PacketName, PacketData);
        }

        public static implicit operator string(MusPacketEvent Packet)
        {
            return Packet.Serialize();
        }
    }
}