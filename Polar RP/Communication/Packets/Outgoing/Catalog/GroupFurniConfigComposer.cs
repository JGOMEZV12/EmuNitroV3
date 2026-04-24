using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;

namespace Polar.Communication.Packets.Outgoing.Catalog
{
    internal class GroupFurniConfigComposer : ServerPacket
    {
        public ICollection<Group> Groups { get; }

        public GroupFurniConfigComposer(ICollection<Group> groups)
            : base(ServerPacketHeader.GroupFurniConfigMessageComposer)
        {
            this.Groups = groups;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Groups.Count);
            foreach (Group Group in Groups)
            {
                packet.WriteInteger(Group.Id);
                packet.WriteString(Group.Name);
                packet.WriteString(Group.Badge);
                base.WriteString(Group.Colour1.ToString());
                base.WriteString(Group.Colour2.ToString());
                //packet.WriteString((PolarEnvironment.GetGame().GetGroupManager().SymbolColours.ContainsKey(Group.Colour1)) ? PolarEnvironment.GetGame().GetGroupManager().SymbolColours[Group.Colour1].Colour : "4f8a00"); // Group Colour 1
                //packet.WriteString((PolarEnvironment.GetGame().GetGroupManager().BackGroundColours.ContainsKey(Group.Colour2)) ? PolarEnvironment.GetGame().GetGroupManager().BackGroundColours[Group.Colour2].Colour : "4f8a00"); // Group Colour 2            
                packet.WriteBoolean(false);
                packet.WriteInteger(Group.CreatorId);
                packet.WriteBoolean(Group.ForumEnabled);
            }
        }
    }
}
