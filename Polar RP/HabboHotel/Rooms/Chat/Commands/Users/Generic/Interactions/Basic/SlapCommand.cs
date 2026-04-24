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
    class SlapCommand : IChatCommand
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
            get { return "Dar una cachetada a otro usuario."; }
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

            // ✅ Fix: validar ambos RoomUsers juntos
            if (RoomUser == null || TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("slap"))
                return;
            
            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("¡No puedes golpear a alguien que no está jugando el juego ahora mismo!", 1);
                return;
            }

            if (Session.GetRoleplay().CurEnergy <= 50)
            {
                Session.SendWhisper("*no tienes energía suficiente para dar cachetada*", 1);
                return;
            }
            #endregion

            #region Execute
            Point ClientPos = new Point(RoomUser.X, RoomUser.Y);
            Point TargetClientPos = new Point(TargetUser.X, TargetUser.Y);
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

            if (Distance <= 1)
            {
                Session.Shout("*Le da una cachetada a  " + TargetClient.GetHabbo().Username + " marcandole los 5 dedos en la mejilla [-50 energía]*", 4);
                RoleplayManager.Shout(TargetClient, "*Se toca la mejilla del dolor que siente " + Session.GetHabbo().Username + "*", 32);
                Session.GetRoleplay().CooldownManager.CreateCooldown("slap", 1000, 3);
                Session.GetRoleplay().CurEnergy -= 50;
                return;
            }
            else
            {
                Session.SendWhisper("¡Debes acercarte a este ciudadano para darles una bofetada!", 1);
                return;
            }
            #endregion
        }
    }
}