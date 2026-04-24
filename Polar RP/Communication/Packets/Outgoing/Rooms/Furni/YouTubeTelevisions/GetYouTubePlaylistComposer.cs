using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Items.Televisions;

namespace Polar.Communication.Packets.Outgoing.Rooms.Furni.YouTubeTelevisions
{
    internal class GetYouTubePlaylistComposer : ServerPacket
    {
        public int ItemId { get; }
        public ICollection<TelevisionItem> Videos { get; }

        public GetYouTubePlaylistComposer(int ItemId, ICollection<TelevisionItem> Videos)
            : base(ServerPacketHeader.GetYouTubePlaylistMessageComposer)
        {
            this.ItemId = ItemId;
            this.Videos = Videos;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(ItemId);
            packet.WriteInteger(Videos.Count);
            foreach (TelevisionItem Video in Videos.ToList())
            {
                packet.WriteString(Video.YouTubeId);
                packet.WriteString(Video.Title);//Title
                packet.WriteString(Video.Description);//Description
            }
            packet.WriteString("");
        }
    }
}
