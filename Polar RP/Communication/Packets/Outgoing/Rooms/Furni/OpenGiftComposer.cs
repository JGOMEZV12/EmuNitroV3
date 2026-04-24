using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Items;

namespace Polar.Communication.Packets.Outgoing.Rooms.Furni
{
    internal class OpenGiftComposer : ServerPacket
    {
        public ItemData Data { get; }
        public string Text { get; }
        public Item Item { get; }
        public bool ItemIsInRoom { get; }

        public OpenGiftComposer(ItemData Data, string Text, Item Item, bool ItemIsInRoom)
            : base(ServerPacketHeader.OpenGiftMessageComposer)
        {
            this.Data = Data;
            this.Text = Text;
            this.Item = Item;
            this.ItemIsInRoom = ItemIsInRoom;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteString(Data.Type.ToString());
            packet.WriteInteger(Data.SpriteId);
            packet.WriteString(Data.ItemName);
            packet.WriteInteger(Item.Id);
            packet.WriteString(Data.Type.ToString());
            packet.WriteBoolean(ItemIsInRoom);//Is it in the room?
            packet.WriteString(Text);
        }
    }
}
