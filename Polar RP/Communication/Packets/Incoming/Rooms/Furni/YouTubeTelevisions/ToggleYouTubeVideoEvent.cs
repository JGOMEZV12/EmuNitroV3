using System;
using System.Linq;
using System.Text;

using Polar.HabboHotel.Items.Televisions;
using Polar.Communication.Packets.Outgoing.Rooms.Furni.YouTubeTelevisions;

namespace Polar.Communication.Packets.Incoming.Rooms.Furni
{
    internal class ToggleYouTubeVideoEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            int ItemId = Packet.PopInt();//Item Id
            string VideoId = Packet.PopString(); //Video ID

            Session.SendMessage(new GetYouTubeVideoComposer(ItemId, VideoId));
        }
    }
}