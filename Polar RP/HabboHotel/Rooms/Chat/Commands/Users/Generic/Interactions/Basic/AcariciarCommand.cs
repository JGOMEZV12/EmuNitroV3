using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;
using System.Drawing;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Basic
{
    class AcariciarCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_interactions_slap"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Dar una caricia a otro usuario."; }
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
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez están sin conexión.", 1);
                return;
            }

            RoomUser RoomUser = Session.GetRoomUser();
            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("acariciar"))
                return;
            
            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("¡No puedes acariciar a alguien que no está jugando el juego ahora mismo!", 1);
                return;
            }

            if (Session.GetRoleplay().CurEnergy <= 10)
            {
                Session.SendWhisper("*no tienes energía suficiente para acariciar*", 1);
                return;
            }
            #endregion

            #region Execute
            Point ClientPos = new Point(RoomUser.X, RoomUser.Y);
            Point TargetClientPos = new Point(TargetUser.X, TargetUser.Y);
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

            if (Distance <= 1)
            {
                Session.Shout("*Acaricia el hermoso rostro de  " + TargetClient.GetHabbo().Username + " y mira fijamente a los ojos [-10 energía]*", 16);
                RoleplayManager.Shout(TargetClient, "*Se ruboriza " + Session.GetHabbo().Username + "*", 16);
                Session.GetRoleplay().CooldownManager.CreateCooldown("acariciar", 1000, 20);
                Session.GetRoleplay().CurEnergy -= 10;
                return;
            }
            else
            {
                Session.SendWhisper("¡Debes acercarte a este ciudadano para acariciarlo!", 1);
                return;
            }
            #endregion
        }
    }
}