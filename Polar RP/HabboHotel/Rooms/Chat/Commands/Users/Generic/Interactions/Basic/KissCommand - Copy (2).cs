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
    class KissCommand2 : IChatCommand
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
            get { return "Besa a otro usuario"; }
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

            if (Session.GetRoleplay().TryGetCooldown("kiss"))
                return;

            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("No se puede besar a alguien que no está jugando el juego ahora mismo", 1);
                return;
            }
            #endregion

            #region Execute
            Point ClientPos = new Point(RoomUser.X, RoomUser.Y);
            Point TargetClientPos = new Point(TargetUser.X, TargetUser.Y);
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

            if (Distance <= 1)
            {
                Session.Shout("*Se inclina hacia " + TargetClient.GetHabbo().Username + " Y les da un pequeño beso en sus labios*", 16);
                Session.GetRoleplay().CooldownManager.CreateCooldown("kiss", 1000, 5);
                RoomUser.ApplyEffect(EffectsList.Love);
                TargetUser.ApplyEffect(EffectsList.Love);
                Session.GetRoleplay().KissTimer = 5;
                TargetClient.GetRoleplay().KissTimer = 5;
                return;
            }
            else
            {
                Session.SendWhisper("¡Debes acercarte a este ciudadano para besarlos!", 1);
                return;
            }
            #endregion
        }
    }
}