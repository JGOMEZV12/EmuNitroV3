using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Navigator
{
    internal class PopularRoomTagsResultComposer : ServerPacket
    {
        public ICollection<KeyValuePair<string, int>> Tags { get; }

        public PopularRoomTagsResultComposer(ICollection<KeyValuePair<string, int>> tags)
            : base(ServerPacketHeader.PopularRoomTagsResultMessageComposer)
        {
            this.Tags = tags;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Tags.Count);
            foreach (KeyValuePair<string, int> tag in Tags)
            {
                packet.WriteString(tag.Key);
                packet.WriteInteger(tag.Value);
            }
        }
    }
}
