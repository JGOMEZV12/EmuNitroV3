using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Items;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    internal class ItemUpdateComposer : ServerPacket
    {

        public ItemUpdateComposer(Item Item, int UserId)
            : base(ServerPacketHeader.ItemUpdateMessageComposer)
        {
            WriteWallItem(Item, UserId);
        }

        private void WriteWallItem(Item Item, int UserId)
        {
            WriteString(Item.Id.ToString());
            WriteInteger(Item.GetBaseItem().SpriteId);
            WriteString(Item.wallCoord);
            ItemBehaviourUtility.GenerateWallExtradata(Item, this);
            WriteInteger(-1);
            WriteInteger((Item.GetBaseItem().Modes > 1) ? 1 : 0);
            WriteInteger(UserId);
            WriteInteger(Item.Data.Stackable ? 1 : 0);
            WriteInteger(Item.Data.IsSeat ? 1 : 0);
            WriteInteger(0);
            WriteInteger(Item.Data.Walkable ? 1 : 0);
            WriteInteger(Item.Data.Width);
            WriteInteger(Item.Data.Length);
            WriteInteger(0);
        }
    }
}