using System;
using System.Threading;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.HabboRoleplay.Houses;
using Polar.HabboRoleplay.Misc;
using System.Drawing;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Apartment
{
    class RideCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_events_leave_game"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Monta un caballo de usuario"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length < 2)
            {
                Session.SendWhisper("Invalid command syntax! :ride <username>", 1);
                return;
            }

            string Username = Convert.ToString(Params[1]);

            RoomUser UserHorsie = Room.GetRoomUserManager().GetRoomUserByHabbo(Username);

            #region Conditions
            if (UserHorsie == null)
            {
                Session.SendWhisper("El usuario no se encuentra o no es un caballo", 1);
                return;
            }

            if (UserHorsie.GetClient() == null)
            {
                Session.SendWhisper("El usuario no se encuentra o no es un caballo!", 1);
                return;
            }
         
            if (UserHorsie.GetClient().GetHabbo() == null)
            {
                Session.SendWhisper("Usuario no encontrado o no es un caballo", 1);
                return;
            }  
            
            if (UserHorsie.GetClient().GetHabbo().PetId <= 0)
            {
                Session.SendWhisper("Este usuario no es un caballo", 1);
                return;
            }
            #endregion


            if (UserHorsie.RidingHorse)
            {
                Session.SendWhisper("Este usuario ya está siendo montado", 1);
                return;
            }

            if (UserHorsie.HorseID == Session.GetRoomUser().VirtualId)
            {
                // unmount
                UserHorsie.Statusses.Remove("sit");
                UserHorsie.Statusses.Remove("lay");
                UserHorsie.Statusses.Remove("snf");
                UserHorsie.Statusses.Remove("eat");
                UserHorsie.Statusses.Remove("ded");
                UserHorsie.Statusses.Remove("jmp");
                Session.GetRoomUser().RidingHorse = false;
                Session.GetRoomUser().HorseID = 0;
                UserHorsie.RidingHorse = false;
                UserHorsie.HorseID = 0;
                Session.GetRoomUser().MoveTo(new Point(Session.GetRoomUser().X + 2, Session.GetRoomUser().Y + 2));
                Session.GetRoomUser().ApplyEffect(-1);
                Session.GetRoomUser().UpdateNeeded = true;
                UserHorsie.UpdateNeeded = true;
            }
            else
            {

                int NewX2 = Session.GetRoomUser().X;
                int NewY2 = Session.GetRoomUser().Y;
                Room.SendMessage(Room.GetRoomItemHandler().UpdateUserOnRoller(UserHorsie, new Point(NewX2, NewY2), 0, Room.GetGameMap().SqAbsoluteHeight(NewX2, NewY2)));
                Room.SendMessage(Room.GetRoomItemHandler().UpdateUserOnRoller(Session.GetRoomUser(), new Point(NewX2, NewY2), 0, Room.GetGameMap().SqAbsoluteHeight(NewX2, NewY2) + 1));

                Session.GetRoomUser().MoveTo(NewX2, NewY2);

                UserHorsie.ClearMovement(true);

                Session.GetRoomUser().RidingHorse = true;
                UserHorsie.RidingHorse = true;
                UserHorsie.HorseID = Session.GetRoomUser().VirtualId;
                Session.GetRoomUser().HorseID = UserHorsie.VirtualId;

                Session.GetRoomUser().ApplyEffect(77);

                Session.GetRoomUser().RotBody = UserHorsie.RotBody;
                Session.GetRoomUser().RotHead = UserHorsie.RotHead;

                Session.GetRoomUser().UpdateNeeded = true;
                UserHorsie.UpdateNeeded = true;


                Session.Shout("*Sube encima de " + UserHorsie.GetClient().GetHabbo().Username + ", ahora eres mi mascota*");
            }
        }
    }
}
