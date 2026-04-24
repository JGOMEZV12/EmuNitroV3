using System;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Rooms.AI;
using Polar.HabboHotel.Rooms;

namespace Polar.Communication.Packets.Outgoing.Rooms.AI.Pets
{
    internal class PetInformationComposer : ServerPacket
    {
        public PetInformationComposer(Pet Pet, bool isMounted = false)
            : base(ServerPacketHeader.PetInformationMessageComposer)
        {
            Room Room;

            if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(Pet.RoomId, out Room))
                return;

            base.WriteInteger(Pet.PetId);
            base.WriteString(Pet.Name);
            base.WriteInteger(Pet.Level);
            base.WriteInteger(Pet.MaxLevel);
            base.WriteInteger(Pet.Experience);
            base.WriteInteger(Pet.ExperienceGoal);
            base.WriteInteger(Pet.Energy);
            base.WriteInteger(Pet.MaxEnergy);
            base.WriteInteger(Pet.Nutrition);
            base.WriteInteger(Pet.MaxNutrition);
            base.WriteInteger(Pet.Respect);
            base.WriteInteger(Pet.OwnerId);
            base.WriteInteger(Pet.Age);
            base.WriteString(Pet.OwnerName);
            base.WriteInteger(1);//3 on hab
            base.WriteBoolean(Pet.Saddle > 0);
            base.WriteBoolean(isMounted);
            base.WriteInteger(0);//5 on hab
            base.WriteInteger(Pet.AnyoneCanRide); // Anyone can ride horse
            base.WriteInteger(0);
            base.WriteInteger(0);//512 on hab
            base.WriteInteger(0);//1536
            base.WriteInteger(0);//2560
            base.WriteInteger(0);//3584
            base.WriteInteger(0);
            base.WriteString("");
            base.WriteBoolean(false);
            base.WriteInteger(-1);//255 on hab
            base.WriteInteger(-1);
            base.WriteInteger(-1);
            base.WriteBoolean(false);
        }
        public PetInformationComposer(Habbo habbo)
             : base(ServerPacketHeader.PetInformationMessageComposer)
        {
            base.WriteInteger(habbo.Id);
                base.WriteString(habbo.Username);
                base.WriteInteger(habbo.Rank);
                base.WriteInteger(10);
                base.WriteInteger(0);
                base.WriteInteger(0);
                base.WriteInteger(100);
                base.WriteInteger(100);
                base.WriteInteger(100);
                base.WriteInteger(100);
                base.WriteInteger(habbo.GetStats().Respect);
                base.WriteInteger(habbo.Id);
                base.WriteInteger(Convert.ToInt32(Math.Floor((PolarEnvironment.GetUnixTimestamp() - habbo.AccountCreated) / 86400)));//How?
                base.WriteString(habbo.Username);
                base.WriteInteger(1);//3 on hab
                base.WriteBoolean(false);
                base.WriteBoolean(false);
                base.WriteInteger(0);//5 on hab
                base.WriteInteger(0); // Anyone can ride horse
                base.WriteInteger(0);
                base.WriteInteger(0);//512 on hab
                base.WriteInteger(0);//1536
                base.WriteInteger(0);//2560
                base.WriteInteger(0);//3584
                base.WriteInteger(0);
                base.WriteString("");
                base.WriteBoolean(false);
                base.WriteInteger(-1);//255 on hab
                base.WriteInteger(-1);
                base.WriteInteger(-1);
                base.WriteBoolean(false);
        }
    }
}
