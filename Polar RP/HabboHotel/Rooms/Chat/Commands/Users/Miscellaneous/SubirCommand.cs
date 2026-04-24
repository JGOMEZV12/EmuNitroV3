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
    class SubirCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_events_subir"; }
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

            RoomUser UserDriving = Room.GetRoomUserManager().GetRoomUserByHabbo(Username);

            #region Conditions
            if (UserDriving == null)
            {
                Session.SendWhisper("El usuario no se encuentra no acepta pasajeros", 1);
                return;
            }

            if (UserDriving.GetClient() == null)
            {
                Session.SendWhisper("este usuario no esta manejando!", 1);
                return;
            }
         
            if (UserDriving.GetClient().GetHabbo() == null)
            {
                Session.SendWhisper("Usuario no encontrado", 1);
                return;
            }  
            
            if (UserDriving.GetClient().GetRoleplay().DrivingCar == false)
            {
                Session.SendWhisper("Este usuario no está conduciendo", 1);
                return;
            }
            #endregion


            if (UserDriving.RidingHorse)
            {
                Session.SendWhisper("Este usuario ya está siendo montado", 1);
                return;
            }

            if (UserDriving.HorseID == Session.GetRoomUser().VirtualId)
            {
                // unmount
                UserDriving.Statusses.Remove("sit");
                UserDriving.Statusses.Remove("lay");
                UserDriving.Statusses.Remove("snf");
                UserDriving.Statusses.Remove("eat");
                UserDriving.Statusses.Remove("ded");
                UserDriving.Statusses.Remove("jmp");
                Session.GetRoomUser().RidingHorse = false;
                Session.GetRoomUser().HorseID = 0;
                UserDriving.RidingCar = false;
                UserDriving.HorseID = 0;
                Session.GetRoomUser().MoveTo(new Point(Session.GetRoomUser().X + 2, Session.GetRoomUser().Y + 2));
                Session.GetRoomUser().ApplyEffect(-1);
                Session.GetRoomUser().UpdateNeeded = true;
                UserDriving.UpdateNeeded = true;
            }
            else
            {

                int NewX2 = Session.GetRoomUser().X;
                int NewY2 = Session.GetRoomUser().Y;
                Room.SendMessage(Room.GetRoomItemHandler().UpdateUserOnRoller(UserDriving, new Point(NewX2, NewY2), 0, Room.GetGameMap().SqAbsoluteHeight(NewX2, NewY2)));
                Room.SendMessage(Room.GetRoomItemHandler().UpdateUserOnRoller(Session.GetRoomUser(), new Point(NewX2, NewY2), 0, Room.GetGameMap().SqAbsoluteHeight(NewX2, NewY2) + 1));

                Session.GetRoomUser().MoveTo(NewX2, NewY2);

                UserDriving.ClearMovement(true);

                Session.GetRoomUser().RidingCar = true;
                UserDriving.RidingCar = true;
                Session.GetRoomUser().ApplyEffect(108);

                Session.GetRoomUser().RotBody = UserDriving.RotBody;
                Session.GetRoomUser().RotHead = UserDriving.RotHead;

                Session.GetRoomUser().UpdateNeeded = true;
                UserDriving.UpdateNeeded = true;


                //Session.Shout("*Hops on " + UserHorsie.GetClient().GetHabbo().Username + ", and starts riding them like their bitch*");
            }
        }
    }
}
