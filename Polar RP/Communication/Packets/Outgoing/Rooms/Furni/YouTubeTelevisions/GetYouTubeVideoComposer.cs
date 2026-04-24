using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Rooms.Furni.YouTubeTelevisions
{
    internal class GetYouTubeVideoComposer : ServerPacket
    {
        public int ItemId { get; }
        public string YouTubeVideo { get; }

        public GetYouTubeVideoComposer(int ItemId, string YouTubeVideo)
            : base(ServerPacketHeader.GetYouTubeVideoMessageComposer)
        {
            this.ItemId = ItemId;
            this.YouTubeVideo = YouTubeVideo;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(ItemId);
            packet.WriteString(YouTubeVideo);//"9Ht5RZpzPqw");
            packet.WriteInteger(0);//Start seconds
            packet.WriteInteger(0);//End seconds
            packet.WriteInteger(0);//State
        }
    }
}
