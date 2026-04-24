using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Rooms.Furni.Stickys
{
    internal class StickyNoteComposer : ServerPacket
    {
        public string ItemId { get; }
        public string ExtraData { get; }

        public StickyNoteComposer(string ItemId, string Extradata)
            : base(ServerPacketHeader.StickyNoteMessageComposer)
        {
            this.ItemId = ItemId;
            this.ExtraData = Extradata;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteString(ItemId);
            packet.WriteString(ExtraData);
        }
    }
}