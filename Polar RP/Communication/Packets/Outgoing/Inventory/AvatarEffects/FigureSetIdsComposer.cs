using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Users.Clothing;

using Polar.HabboHotel.Users.Clothing.Parts;

namespace Polar.Communication.Packets.Outgoing.Inventory.AvatarEffects
{
    internal class FigureSetIdsComposer : ServerPacket
    {
        public ICollection<ClothingParts> ClothingParts { get; }

        public FigureSetIdsComposer(ICollection<ClothingParts> ClothingParts)
            : base(ServerPacketHeader.FigureSetIdsMessageComposer)
        {
            this.ClothingParts = ClothingParts;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(ClothingParts.Count);
            foreach (ClothingParts Part in ClothingParts.ToList())
            {
                packet.WriteInteger(Part.PartId);
            }

            packet.WriteInteger(ClothingParts.Count);
            foreach (ClothingParts Part in ClothingParts.ToList())
            {
                packet.WriteString(Part.Part);
            }
        }
    }
}
