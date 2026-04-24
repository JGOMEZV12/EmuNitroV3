using Polar.Communication.Packets.Outgoing.Rooms.AI.Pets;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users.Inventory.Bots;
using Polar.HabboRoleplay.Combat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Polar.Communication.Packets.Incoming.Rooms.AI.Pets
{
    internal class GetPetInformationEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            if (!Session.GetHabbo().InRoom)
                return;

            int PetId = Packet.PopInt();

            RoomUser Pet = null;
            if (!Session.GetHabbo().CurrentRoom.GetRoomUserManager().TryGetPet(PetId, out Pet))
            {
                int LookupId = PetId;
                if (LookupId > 1000000) LookupId -= 1000000;

                if (Session.GetHabbo().CurrentRoom.GetRoomUserManager().TryGetBot(LookupId, out Pet))
                {
                    if (Pet.IsBot && Pet.PetData != null)
                    {
                        if (Session.GetRoleplay().EquippedWeapon == null)
                        {
                            CombatManager.GetCombatType("fist").ExecuteBot(Session, Pet.GetBotRoleplay());
                        }
                        else
                        {
                            CombatManager.GetCombatType("gun").ExecuteBot(Session, Pet.GetBotRoleplay());
                        }
                        Session.SendMessage(new PetInformationComposer(Pet.PetData, Pet.PetData.Type == 15 ? Pet.RidingHorse : false));
                        return;
                    }
                }
                //Okay so, we've established we have no pets in this room by this virtual Id, let us check out users, maybe they're creeping as a pet?!
                RoomUser User = Session.GetHabbo().CurrentRoom.GetRoomUserManager().GetRoomUserByHabbo(PetId);
                if (User == null)
                    return;

                //Check some values first, please!
                if (User.GetClient() == null || User.GetClient().GetHabbo() == null)
                    return;

                //And boom! Let us send the information composer 8-).
                Session.SendMessage(new PetInformationComposer(User.GetClient().GetHabbo()));
                return;
            }

            //Continue as a regular pet..
            if (Pet.RoomId != Session.GetHabbo().CurrentRoomId || Pet.PetData == null)
                return;

            Session.SendMessage(new PetInformationComposer(Pet.PetData, Pet.PetData.Type == 15 ? Pet.RidingHorse : false));
        }
    }
}
