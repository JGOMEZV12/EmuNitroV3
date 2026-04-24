using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Users.Messenger;

namespace Polar.Communication.Packets.Outgoing.Messenger
{
    internal class HabboSearchResultComposer : ServerPacket
    {
        public List<SearchResult> Friends { get; }
        public List<SearchResult> OtherUsers { get; }

        public HabboSearchResultComposer(List<SearchResult> Friends, List<SearchResult> OtherUsers)
            : base(ServerPacketHeader.HabboSearchResultMessageComposer)
        {
            this.Friends = Friends;
            this.OtherUsers = OtherUsers;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Friends.Count);
            foreach (SearchResult Friend in Friends.ToList())
            {
                bool Online = (PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Friend.UserId) != null);

                packet.WriteInteger(Friend.UserId);
               packet.WriteString(Friend.Username);
               packet.WriteString(Friend.Motto);
                packet.WriteBoolean(Online);
                packet.WriteBoolean(false);
               packet.WriteString(string.Empty);
                packet.WriteInteger(0);
               packet.WriteString(Online ? Friend.Figure : "");
               packet.WriteString(Friend.LastOnline);
            }

            packet.WriteInteger(OtherUsers.Count);
            foreach (SearchResult OtherUser in OtherUsers.ToList())
            {
                bool Online = (PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(OtherUser.UserId) != null);

                packet.WriteInteger(OtherUser.UserId);
               packet.WriteString(OtherUser.Username);
               packet.WriteString(OtherUser.Motto);
                packet.WriteBoolean(Online);
                packet.WriteBoolean(false);
               packet.WriteString(string.Empty);
                packet.WriteInteger(0);
               packet.WriteString(Online ? OtherUser.Figure : "");
               packet.WriteString(OtherUser.LastOnline);
            }
        }
    }
}
