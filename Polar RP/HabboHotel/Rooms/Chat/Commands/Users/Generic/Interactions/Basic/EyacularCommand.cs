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
    class EyacularCommand : IChatCommand
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
            get { return "Acabarle/eyacularle a alguien."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Vaya, se le olvidó ingresar un nombre de usuario", 1);
                return;
            }

            if (Session.GetRoleplay().CurEnergy < 5)
            {
                Session.SendWhisper("Necesitas al menos 5% de energía para hacer esto", 1);
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

            if (Session.GetRoleplay().TryGetCooldown("eyacular"))
                return;
            
            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("¡No puedes eyacular a alguien que no está jugando el juego ahora mismo!", 1);
                return;
            }
            #endregion

            #region Execute
            Point ClientPos = new Point(RoomUser.X, RoomUser.Y);
            Point TargetClientPos = new Point(TargetUser.X, TargetUser.Y);
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

            if (Distance <= 1)
            {
                Session.Shout("*Se masturba para acabarle encima a  " + TargetClient.GetHabbo().Username + " y lo logra [-5] Energía*", 5);
                RoleplayManager.Shout(TargetClient, "*Se siente asqueros@ porque " + Session.GetHabbo().Username + " le eyaculó en la cara [-2] Higiene*", 5);
                RoomUser.ApplyEffect(507);
                Session.GetRoleplay().SexTimer = 10;
                Session.GetRoleplay().CurEnergy -= 5;
                TargetClient.GetRoleplay().Hygiene -= 2;
                Session.GetRoleplay().CooldownManager.CreateCooldown("eyacular", 1000, 3);
                return;
            }
            else
            {
                Session.SendWhisper("¡Debes acercarte a este ciudadano para acabarle!", 1);
                return;
            }
            #endregion
        }
    }
}