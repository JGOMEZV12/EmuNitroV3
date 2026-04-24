using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Groups;

namespace Polar.Communication.Packets.Incoming.Groups
{
    internal class GetBadgeEditorPartsEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            Session.SendMessage(new BadgeEditorPartsComposer(
                PolarEnvironment.GetGame().GetGroupManager().Bases,
                PolarEnvironment.GetGame().GetGroupManager().Symbols,
                PolarEnvironment.GetGame().GetGroupManager().BaseColours,
                PolarEnvironment.GetGame().GetGroupManager().SymbolColours,
                PolarEnvironment.GetGame().GetGroupManager().BackGroundColours));
       
        }
    }
}
