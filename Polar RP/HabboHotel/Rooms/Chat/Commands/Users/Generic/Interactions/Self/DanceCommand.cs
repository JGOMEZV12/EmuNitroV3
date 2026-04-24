using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Rooms.Avatar;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Self
{
    class DanceCommand :IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_interactions_dance"; }
        }

        public string Parameters
        {
            get { return "%danceid%"; }
        }

        public string Description
        {
            get { return "Begins dancing based on the dance id."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            RoomUser ThisUser = Session.GetRoomUser();
            if (ThisUser == null)
                return;

            if (Session.GetRoleplay().DrivingCar || Session.GetRoleplay().Pasajero || Session.GetRoleplay().EquippedWeapon != null
                || Session.GetRoleplay().WateringCan || Session.GetRoleplay().IsDead)
                return;

            if (Params.Length == 1)
            {
                Session.SendWhisper("Please enter an ID of a dance.", 1);
                return;
            }

            int DanceId;
            if (int.TryParse(Params[1], out DanceId))
            {
                if (DanceId > 4 || DanceId < 0)
                {
                    Session.SendWhisper("The dance ID must be between 1 and 4!", 1);
                    return;
                }

                Session.GetHabbo().CurrentRoom.SendMessage(new DanceComposer(ThisUser, DanceId));
            }
            else
                Session.SendWhisper("Please enter a valid dance ID.", 1);
        }
    }
}
