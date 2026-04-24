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
    class MasajeCommand : IChatCommand
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
            get { return "Darle un masaje a otro usuario"; }
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

            if (Session.GetRoleplay().TryGetCooldown("masaje"))
                return;

            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("No se puede darle masaje a alguien que no está jugando el juego ahora mismo", 1);
                return;
            }

            if (Session.GetRoleplay().CurEnergy < 10)
            {
                Session.SendWhisper("No puedes darle masaje a otra persona porque no tienes energía para hacerlo", 1);
                return;
            }

            if (Session.GetRoleplay().EquippedWeapon != null)
            {
                Session.SendWhisper("¿Como piensas dar un masaje con las manos ocupadas? GUARDA EL ARMA", 1);
                return;
            }

            if (TargetClient.GetRoleplay().CurEnergy >= TargetClient.GetRoleplay().MaxEnergy)
            {
                Session.Shout("* " + TargetClient.GetHabbo().Username + " ¡Oh, Lo siento. ya estás muy relajado!", 15);
                return;
            }

            if (TargetClient == Session)
            {
                Session.SendWhisper("¡No puedes darte masaje a ti mismo!", 1);
                return;
            }
            #endregion

            #region Execute
            Point ClientPos = new Point(RoomUser.X, RoomUser.Y);
            Point TargetClientPos = new Point(TargetUser.X, TargetUser.Y);
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

            if (Distance <= 1)
            {
                Session.Shout("*Agarra a " + TargetClient.GetHabbo().Username + " y comienza a darle un masaje [-10 energía]*", 4);
                TargetClient.Shout("*Uuuff, aaaahh que rico ya me siento mucho mejor [+10 Energía]*", 4);
                TargetClient.GetRoleplay().CurEnergy += 10;
                Session.GetRoleplay().CurEnergy -= 10;
                Session.GetRoleplay().CooldownManager.CreateCooldown("kiss", 1000, 5);
                Session.GetRoleplay().KissTimer = 5;
                TargetClient.GetRoleplay().KissTimer = 5;
                return;
            }
            else
            {
                Session.SendWhisper("¡Debes acercarte a este ciudadano para darle un masaje!", 1);
                return;
            }
            #endregion
        }
    }
}