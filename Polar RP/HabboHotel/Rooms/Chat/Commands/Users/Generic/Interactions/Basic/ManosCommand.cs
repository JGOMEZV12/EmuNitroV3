using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;
using System.Drawing;
using Polar.HabboHotel.Users.Effects;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Basic
{
    class ManosCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_interactions_kiss"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Agarra de la mano a otra persona"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Vaya, se le olvidó ingresar un nombre de usuario", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez estén sin conexión.", 1);
                return;
            }

            RoomUser RoomUser = Session.GetRoomUser();
            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("manos"))
                return;

            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("No se puede darle la mano a alguien que no está jugando el juego ahora mismo", 1);
                return;
            }

            if (Session.GetRoleplay().EquippedWeapon != null)
            {
                Session.SendWhisper("¿Como piensas agarras de las manos si estan ocupadas? GUARDA EL ARMA", 1);
                return;
            }
            #endregion

            #region Execute
            Point ClientPos = new Point(RoomUser.X, RoomUser.Y);
            Point TargetClientPos = new Point(TargetUser.X, TargetUser.Y);
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

            if (Distance <= 1)
            {
                Session.Shout("*Agarra a " + TargetClient.GetHabbo().Username + " de la mano [-10 energía]*", 4);
                Session.GetRoleplay().CurEnergy -= 10;
                Session.GetRoleplay().CooldownManager.CreateCooldown("manos", 1000, 5);
                Session.GetRoleplay().KissTimer = 5;
                TargetClient.GetRoleplay().KissTimer = 5;
                return;
            }
            else
            {
                Session.SendWhisper("¡Debes acercarte a este ciudadano para agarrarle la mano!", 1);
                return;
            }
            #endregion
        }
    }
}