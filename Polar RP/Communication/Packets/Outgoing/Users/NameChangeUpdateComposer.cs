using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Users
{
    internal class NameChangeUpdateComposer : ServerPacket
    {
        public int Error { get; }
        public string Name { get; }
        public ICollection<string> Tags { get; }
        public NameChangeUpdateComposer(string name, int error, ICollection<string> tags)
            : base(ServerPacketHeader.NameChangeUpdateMessageComposer)
        {
            this.Error = error;
            this.Name = name;
            this.Tags = tags;
            Compose(this);
        }

        public NameChangeUpdateComposer(string name, int error)
            : base(ServerPacketHeader.NameChangeUpdateMessageComposer)
        {
            this.Error = error;
            this.Name = name;
            this.Tags = new List<string>();
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Error);
            packet.WriteString(Name);

            packet.WriteInteger(Tags.Count);
            foreach (string tag in Tags)
            {
                packet.WriteString(Name + tag);
            }
        }
    }
}
