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
    class AgarratetaCommand : IChatCommand
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
            get { return "Agarre la teta o pene de una ciudadana"; }
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
                Session.SendWhisper("* no tienes energía siquiera para llorar *", 1);
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
                    Session.Shout("*Agarra las tetas de  " + TargetClient.GetHabbo().Username + " y la acaricia con sus dedos [-5 Energía]*", 16);
                    RoleplayManager.Shout(TargetClient, "*Gime y tiembla, también se sonroja porque " + Session.GetHabbo().Username + " la tocó*", 16);
                    Session.GetRoleplay().CooldownManager.CreateCooldown("agarrarteta", 1000, 60);
                    Session.GetRoleplay().CurEnergy -= 25;
                    TargetClient.GetRoomUser().ApplyEffect(910);

                }
                else if (Session.GetHabbo().Gender.ToLower().StartsWith("f") && TargetClient.GetHabbo().Gender.ToLower().StartsWith("m"))
                {
                    Session.Shout("*Desliza su mano y le agarra el pene a " + TargetClient.GetHabbo().Username + " y se lo apreta [-5 Energía]*", 16);
                    RoleplayManager.Shout(TargetClient, "*Se sonroja porque " + Session.GetHabbo().Username + " le agarró su pene*", 16);
                    Session.GetRoleplay().CooldownManager.CreateCooldown("agarrarteta", 1000, 60);
                    Session.GetRoleplay().CurEnergy -= 25;
                    TargetClient.GetRoomUser().ApplyEffect(910);

                }
                else if (Session.GetHabbo().Gender.ToLower().StartsWith("m") && TargetClient.GetHabbo().Gender.ToLower().StartsWith("m"))
                {
                    Session.Shout("*Desliza su mano y le agarra el pene a " + TargetClient.GetHabbo().Username + " y se lo apreta [-5 Energía]*", 16);
                    RoleplayManager.Shout(TargetClient, "*Se sonroja porque " + Session.GetHabbo().Username + " le agarró su pene*", 16);
                    Session.GetRoleplay().CooldownManager.CreateCooldown("agarrarteta", 1000, 60);
                    Session.GetRoleplay().CurEnergy -= 25;
                    TargetClient.GetRoomUser().ApplyEffect(910);
                }
                else if (Session.GetHabbo().Gender.ToLower().StartsWith("f") && TargetClient.GetHabbo().Gender.ToLower().StartsWith("f"))
                {
                    Session.Shout("*Agarra las tetas de  " + TargetClient.GetHabbo().Username + " y la acaricia con sus dedos [-5 Energía]*", 16);
                    RoleplayManager.Shout(TargetClient, "*Gime y tiembla, también se sonroja porque " + Session.GetHabbo().Username + " la tocó*", 16);
                    Session.GetRoleplay().CooldownManager.CreateCooldown("agarrarteta", 1000, 60);
                    Session.GetRoleplay().CurEnergy -= 25;
                    TargetClient.GetRoomUser().ApplyEffect(910);
                }
                else
                {
                    Session.Shout("*Hace el amor rico a " + TargetClient.GetHabbo().Username + "*", 16);
                    RoleplayManager.Shout(TargetClient, "*Ay si, dame de lo tuyo " + Session.GetHabbo().Username + " ¡así así!*", 16);
                }

            else
            {
                Session.SendWhisper("¡Debes acercarte a este ciudadano para tocarlo!", 1);
                return;
            }
            #endregion
        }
    }
}