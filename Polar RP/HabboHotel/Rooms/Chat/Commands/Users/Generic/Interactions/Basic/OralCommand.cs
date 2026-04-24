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
    class OralCommand : IChatCommand
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
            get { return "Hagale oral a otro usuario"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Vaya, se le olvidó ingresar un nombre de usuario", 1);
                return;
            }

            if (Session.GetRoleplay().IsWorking)
            {
                Session.SendWhisper("No puedes hacer esto a nadie en pleno trabajo ¡Sinverguenza!", 1);
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
                Session.SendWhisper("No se puede mamarselo a alguien que no está jugando el juego ahora mismo", 1);
                return;
            }

            if (Session.GetRoleplay().CurEnergy <= 25)
            {
                Session.SendWhisper("*no tienes energía suficiente para dar cachetada*", 1);
                return;
            }
            if (Session.GetRoleplay().PassiveMode)
            {
                Session.SendWhisper("No puedes realizar acciones sexuales en modo pasivo.", 1);
                return;
            }

            if (TargetClient.GetRoleplay().PassiveMode)
            {
                Session.SendWhisper("¡Este usuario tiene el modo pasivo activo!.", 1);
                return;
            }

            #endregion

            #region Execute
            Point ClientPos = new Point(RoomUser.X, RoomUser.Y);
            Point TargetClientPos = new Point(TargetUser.X, TargetUser.Y);
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

            if (Distance <= 1)


                if (Session.GetHabbo().Gender.ToLower().StartsWith("m") && TargetClient.GetHabbo().Gender.ToLower().StartsWith("f"))
                {
                    Session.Shout("*Agarra a " + TargetClient.GetHabbo().Username + " le quita el pantalón y se agacha y lame su clitoris*", 16);
                    RoleplayManager.Shout(TargetClient, "*Gime y tiembla porque " + Session.GetHabbo().Username + " le hace sexo oral*", 16);
                    Session.GetRoleplay().CooldownManager.CreateCooldown("sex", 1000, 60);
                    Session.GetRoleplay().CurEnergy -= 25;
                    RoomUser.ApplyEffect(507);
                }
                else if (Session.GetHabbo().Gender.ToLower().StartsWith("f") && TargetClient.GetHabbo().Gender.ToLower().StartsWith("m"))
                {
                    Session.Shout("*Empuja a " + TargetClient.GetHabbo().Username + " se agacha le baja el pantalón y comienza a besarle el pene*", 16);
                    RoleplayManager.Shout(TargetClient, "*Gime y agarra la cabeza de " + Session.GetHabbo().Username + " empujandola con las manos hacia delante y hacia atrás*", 16);
                    Session.GetRoleplay().CooldownManager.CreateCooldown("sex", 1000, 60);
                    Session.GetRoleplay().CurEnergy -= 25;
                    RoomUser.ApplyEffect(507);
                }
                else if (Session.GetHabbo().Gender.ToLower().StartsWith("m") && TargetClient.GetHabbo().Gender.ToLower().StartsWith("m"))
                {
                    Session.Shout("*Agarra a " + TargetClient.GetHabbo().Username + " por el pelo y hace que se atragante con su pene*", 16);
                    RoleplayManager.Shout(TargetClient, "*Se ahoga " + Session.GetHabbo().Username + " y hace sonidos extraños*", 16);
                    Session.GetRoleplay().CooldownManager.CreateCooldown("sex", 1000, 60);
                    Session.GetRoleplay().CurEnergy -= 25;
                    RoomUser.ApplyEffect(507);
                }
                else if (Session.GetHabbo().Gender.ToLower().StartsWith("f") && TargetClient.GetHabbo().Gender.ToLower().StartsWith("f"))
                {
                    Session.Shout("*Empuja a " + TargetClient.GetHabbo().Username + " y comienza a chupar toda su vagina*", 16);
                    RoleplayManager.Shout(TargetClient, "*Gime y tiembla como " + Session.GetHabbo().Username + " se la come completa*", 16);
                    Session.GetRoleplay().CooldownManager.CreateCooldown("sex", 1000, 60);
                    Session.GetRoleplay().CurEnergy -= 25;
                    RoomUser.ApplyEffect(507);
                }
                else
                {
                    Session.Shout("*Hace el amor rico a " + TargetClient.GetHabbo().Username + "*", 16);
                    RoleplayManager.Shout(TargetClient, "*Ay si, dame de lo tuyo " + Session.GetHabbo().Username + " ¡así así!*", 16);
                }

            else
            {
                Session.SendWhisper("¡Debes acercarte a este ciudadano para mamarselo!", 1);
                return;
            }
            #endregion
        }
    }
}