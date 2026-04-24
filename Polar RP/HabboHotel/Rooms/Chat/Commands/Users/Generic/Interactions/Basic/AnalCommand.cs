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
    class AnalCommand : IChatCommand
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
            get { return "Sexo anal a otro usuario"; }
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

            if (Session == TargetClient)
            {
                Session.SendWhisper("¡No hacerte anal a ti mismo!", 1);
                return;
            }

            RoomUser RoomUser = Session.GetRoomUser();
            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("Anal"))
                return;

            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("No se puede hacerse sexo anal a alguien que no está jugando ahora mismo", 1);
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

            if (Session.GetRoleplay().CurEnergy <= 50)
            {
                Session.SendWhisper("*no tienes energía suficiente para anal*", 1);
                return;
            }
            #endregion

            #region Execute
            Point ClientPos = new Point(RoomUser.X, RoomUser.Y);
            Point TargetClientPos = new Point(TargetUser.X, TargetUser.Y);
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

            if (Distance <= 1)
            {
                if (Session.GetHabbo().Gender.ToLower().StartsWith("m") && TargetClient.GetHabbo().Gender.ToLower().StartsWith("f"))
                {
                    Session.Shout("*Agarra a " + TargetClient.GetHabbo().Username + " le escupe el culo y comienza a introducir su pene*", 16);
                    RoleplayManager.Shout(TargetClient, "*llora del dolor porque " + Session.GetHabbo().Username + " Empuja su pene dentro de ella muy fuerte y la rompe*", 16);
                }
                else if (Session.GetHabbo().Gender.ToLower().StartsWith("f") && TargetClient.GetHabbo().Gender.ToLower().StartsWith("m"))
                {
                    Session.Shout("*Empuja a " + TargetClient.GetHabbo().Username + " Abajo en el piso, le quita el pantalon y mete sus dedos en su culo*", 16);
                    RoleplayManager.Shout(TargetClient, "*LLora del dolor porque " + Session.GetHabbo().Username + "'s mete los dedos dentro de el*", 16);
                    
                }
                else if (Session.GetHabbo().Gender.ToLower().StartsWith("m") && TargetClient.GetHabbo().Gender.ToLower().StartsWith("m"))
                {
                    Session.Shout("*Agarra a " + TargetClient.GetHabbo().Username + " le quita el pantalon y se baja el cierre e introduce su pene*", 16);
                    RoleplayManager.Shout(TargetClient, "*Llora del dolor porque " + Session.GetHabbo().Username + " le da muy duro*", 16);
                    
                }
                else if (Session.GetHabbo().Gender.ToLower().StartsWith("f") && TargetClient.GetHabbo().Gender.ToLower().StartsWith("f"))
                {
                    Session.Shout("*Empuja a " + TargetClient.GetHabbo().Username + " la inclina, le escupe el culo e introduce sus dedos*", 16);
                    RoleplayManager.Shout(TargetClient, "*Gime y tiembla como " + Session.GetHabbo().Username + "'s mete los dedos en su culo*", 16);
                }
                else
                {
                    Session.Shout("*Hace el amor rico a " + TargetClient.GetHabbo().Username + "*", 16);
                    RoleplayManager.Shout(TargetClient, "*Ay si, dame de lo tuyo " + Session.GetHabbo().Username + "'s ¡Muevete así!*", 16);
                }
                Session.GetRoleplay().CooldownManager.CreateCooldown("sex", 1000, 60);
                Session.GetRoleplay().CurEnergy -= 50;
                Session.GetRoleplay().Sida += 5;
                RoomUser.ApplyEffect(507);
                TargetUser.ApplyEffect(507);
                Session.GetRoleplay().SexTimer = 15;
                TargetClient.GetRoleplay().SexTimer = 15;
                RoleplayManager.GetLookAndMotto(Session);
                RoleplayManager.GetLookAndMotto(TargetClient);
                return;
            }
            else
            {
                Session.SendWhisper("¡Debes acercarte a este ciudadano para tener relaciones sexuales con ellos!", 1);
                return;
            }
            #endregion
        }
    }
}