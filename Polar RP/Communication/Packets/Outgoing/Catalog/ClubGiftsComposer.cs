using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Catalog
{
    internal class ClubGiftsComposer : ServerPacket
    {
        public ClubGiftsComposer()
            : base(ServerPacketHeader.ClubGiftsMessageComposer)
        {
            Compose(this);
        }
        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(31); // Days until next gift.
            packet.WriteInteger(0); // Gifts available
            packet.WriteInteger(1); // Count?
            {
                packet.WriteInteger(200054);
                packet.WriteString("CFC_100000_diam");
                packet.WriteBoolean(false);
                packet.WriteInteger(5);
                packet.WriteInteger(0);
                packet.WriteInteger(0);
                packet.WriteBoolean(true);
                packet.WriteInteger(1); // Count for some reason
                {
                    packet.WriteString("s");
                    packet.WriteInteger(200054);
                    packet.WriteString("");
                    packet.WriteInteger(1);
                    packet.WriteBoolean(false);
                }
                packet.WriteInteger(0);
                packet.WriteBoolean(false);
                packet.WriteBoolean(false);
                packet.WriteString(String.Empty);
            }

            packet.WriteInteger(1);//Count
            {
                //int, bool, int, bool
                packet.WriteInteger(200054);//Maybe the item id?
                packet.WriteBoolean(true);//Can we get?
                packet.WriteInteger(-1);//idk
                packet.WriteBoolean(true);//idk
            }
        }
    }
}
