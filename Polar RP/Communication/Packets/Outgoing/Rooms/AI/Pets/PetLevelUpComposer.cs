using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Polar.HabboHotel.Rooms.AI;

namespace Polar.Communication.Packets.Outgoing.Rooms.AI.Pets
{
    internal class PetLevelUpComposer : ServerPacket
    {
        public PetLevelUpComposer(Pet pet)
            : base(ServerPacketHeader.PetLevelUpMessageComposer)
        {
            base.WriteInteger(pet.PetId);
            base.WriteInteger(pet.VirtualId);
            base.WriteInteger(pet.Level);
        }
    }
}