using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Rooms.AI.Pets
{
    internal class AddExperiencePointsComposer : ServerPacket
    {
        public int PetId { get; }
        public int VirtualId { get; }
        public int Amount { get; }

        public AddExperiencePointsComposer(int PetId, int VirtualId, int Amount)
            : base(ServerPacketHeader.AddExperiencePointsMessageComposer)
        {
            this.PetId = PetId;
            this.VirtualId = VirtualId;
            this.Amount = Amount;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(PetId);
            packet.WriteInteger(VirtualId);
            packet.WriteInteger(Amount);
        }
    }
}