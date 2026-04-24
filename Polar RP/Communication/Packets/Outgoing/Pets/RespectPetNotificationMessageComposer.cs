using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Users;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.AI;

namespace Polar.Communication.Packets.Outgoing.Pets
{
    internal class RespectPetNotificationMessageComposer : ServerPacket
    {
        public Pet Pet { get; }
        public RoomUser RoomUser { get; }
        public Habbo Habbo { get; }

        public RespectPetNotificationMessageComposer(Pet Pet)
            : base(ServerPacketHeader.RespectPetNotificationMessageComposer)
        {
            this.Pet = Pet;
            Compose(this);
        }

        public RespectPetNotificationMessageComposer(Habbo habbo, RoomUser User)
            : base(ServerPacketHeader.RespectPetNotificationMessageComposer)
        {
            this.RoomUser = User;
            this.Habbo = habbo;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            if (Pet != null)
            {
                //TODO: Structure
                packet.WriteInteger(Pet.VirtualId);
                packet.WriteInteger(Pet.VirtualId);
                packet.WriteInteger(Pet.PetId);//Pet Id, 100%
                packet.WriteString(Pet.Name);
                packet.WriteInteger(0);
                packet.WriteInteger(0);
                packet.WriteString(Pet.Color);
                packet.WriteInteger(0);
                packet.WriteInteger(0);//Count - 3 ints.
                packet.WriteInteger(1);
            }
            else if (RoomUser != null)
            {
                //TODO: Structure
                packet.WriteInteger(RoomUser.VirtualId);
                packet.WriteInteger(RoomUser.VirtualId);
                packet.WriteInteger(Habbo.Id);//Pet Id, 100%
                packet.WriteString(Habbo.Username);
                packet.WriteInteger(0);
                packet.WriteInteger(0);
                packet.WriteString("FFFFFF");//Yeah..
                packet.WriteInteger(0);
                packet.WriteInteger(0);//Count - 3 ints.
                packet.WriteInteger(1);
            }
        }
    }
}
