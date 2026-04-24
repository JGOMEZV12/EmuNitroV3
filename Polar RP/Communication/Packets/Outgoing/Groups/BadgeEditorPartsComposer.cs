using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;

namespace Polar.Communication.Packets.Outgoing.Groups
{
    internal class BadgeEditorPartsComposer : ServerPacket
    {
        public ICollection<GroupBases> Bases { get; }
        public ICollection<GroupSymbols> Symbols { get; }
        public ICollection<GroupBaseColours> BaseColours { get; }
        public Dictionary<int, GroupSymbolColours> SymbolColours { get; }
        public Dictionary<int, GroupBackGroundColours> BackgroundColours { get; }
        public BadgeEditorPartsComposer(ICollection<GroupBases> bases, ICollection<GroupSymbols> symbols, ICollection<GroupBaseColours> baseColours, Dictionary<int, GroupSymbolColours> symbolColours, Dictionary<int, GroupBackGroundColours> backgroundColours)
            : base(ServerPacketHeader.BadgeEditorPartsMessageComposer)
        {
            this.Bases = bases;
            this.Symbols = symbols;
            this.BaseColours = baseColours;
            this.SymbolColours = symbolColours;
            this.BackgroundColours = backgroundColours;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Bases.Count);
            foreach (GroupBases Item in Bases)
            {
                packet.WriteInteger(Item.Id);
               packet.WriteString(Item.Value1);
               packet.WriteString(Item.Value2);
            }

            packet.WriteInteger(Symbols.Count);
            foreach (GroupSymbols Item in Symbols)
            {
                packet.WriteInteger(Item.Id);
               packet.WriteString(Item.Value1);
               packet.WriteString(Item.Value2);
            }

            packet.WriteInteger(BaseColours.Count);
            foreach (GroupBaseColours Colour in BaseColours)
            {
                packet.WriteInteger(Colour.Id);
               packet.WriteString(Colour.Colour);
            }

            packet.WriteInteger(SymbolColours.Count);
            foreach (GroupSymbolColours Colour in SymbolColours.Values.ToList())
            {
                packet.WriteInteger(Colour.Id);
               packet.WriteString(Colour.Colour);
            }

            packet.WriteInteger(BackgroundColours.Count);
            foreach (GroupBackGroundColours Colour in BackgroundColours.Values.ToList())
            {
                packet.WriteInteger(Colour.Id);
               packet.WriteString(Colour.Colour);
            }
        }
    }
}