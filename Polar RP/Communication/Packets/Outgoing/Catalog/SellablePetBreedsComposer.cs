using System;
using System.Linq;
using System.Collections.Generic;

using Polar.HabboHotel.Catalog.Pets;

namespace Polar.Communication.Packets.Outgoing.Catalog
{
    public class SellablePetBreedsComposer : ServerPacket
    {
        public SellablePetBreedsComposer(string petType, int petId, ICollection<PetRace> Races)
            : base(ServerPacketHeader.SellablePetBreedsMessageComposer)
        {
            base.WriteString(petType);

            base.WriteInteger(Races.Count);
            foreach (PetRace Race in Races.ToList())
            {
                base.WriteInteger(petId);
                base.WriteInteger(Race.PrimaryColour);
                base.WriteInteger(Race.SecondaryColour);
                base.WriteBoolean(Race.HasPrimaryColour);
                base.WriteBoolean(Race.HasSecondaryColour);
            }
        }
    }
}