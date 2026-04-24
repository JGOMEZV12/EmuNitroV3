using Polar.HabboHotel.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    internal class SlideObjectBundleComposer : ServerPacket
    {
        public SlideObjectBundleComposer(int fromX, int fromY, double fromZ, int toX, int toY, double toZ, int rollerId, int avatarId, int itemId)
            : base(ServerPacketHeader.SlideObjectBundleMessageComposer)
        {
            bool IsItem = itemId > 0;

           base.WriteInteger(fromX);
           base.WriteInteger(fromY);
           base.WriteInteger(toX);
           base.WriteInteger(toY);
           base.WriteInteger(IsItem ? 1 : 0);

            if (IsItem)
               base.WriteInteger(itemId);
            else
            {
               base.WriteInteger(rollerId);
               base.WriteInteger(2);
               base.WriteInteger(avatarId);
            }

           base.WriteString(fromZ.ToString().Replace(',', '.'));
           base.WriteString(toZ.ToString().Replace(',', '.'));

            if (IsItem)
            {
               base.WriteInteger(rollerId);
            }
        }
    }
}
