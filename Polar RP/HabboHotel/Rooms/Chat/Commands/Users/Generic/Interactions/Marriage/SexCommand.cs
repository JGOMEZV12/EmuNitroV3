using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;
using System.Drawing;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Marriage
{
    class SexCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_interactions_sex"; }
        }

        public string Parameters
        {
            get { return "%Username%"; }
        }

        public string Description
        {
            get { return "Tener relaciones sexuales."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("¡Vaya, se le olvidó ingresar un nombre de usuario!", 16);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez estén sin conexión.", 16);
                return;
            }

            RoomUser RoomUser = Session.GetRoomUser();
            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 16);
                return;
            }

            if (Session.GetRoleplay().CurEnergy <= 0)
            {
                Session.SendWhisper("¡No tienes suficiente energía para tener sexo, ve a tomar una cerveza!", 1);
                return;
            }

            if (Session.GetRoleplay().MarriedTo != TargetClient.GetHabbo().Id)
            {
                Session.SendWhisper("¡Solo puedes tener sexo con tu pareja!", 1);
                return;
            }

            if (Session.GetRoleplay().MarriedTo == 0)
            {
                Session.SendWhisper("¡Debes casarte para poder tener sexo!", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("sex"))
                return;

            if (TargetClient.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes tener sexo con alguien que está muerto!", 16);
                return;
            }


            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("¡No puedes tener relaciones sexuales con alguien que no está jugando el juego ahora mismo!", 16);
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
                    Session.Shout("*Agarra a " + TargetClient.GetHabbo().Username + " Por el pecho y la aborda, quitándose la ropa*", 16);
                    RoleplayManager.Shout(TargetClient, "*Gime y tiembla porque " + Session.GetHabbo().Username + " Empuja su pene dentro de ella*", 16);
                    TargetClient.SendWhisper("Gracias al sexo que tuviste tu felicidad está al máximo");
                    Session.SendWhisper("Gracias al sexo que tuviste tu felicidad está al máximo");
                }
                else if (Session.GetHabbo().Gender.ToLower().StartsWith("f") && TargetClient.GetHabbo().Gender.ToLower().StartsWith("m"))
                {
                    Session.Shout("*Empuja a " + TargetClient.GetHabbo().Username + " Abajo en el piso, subiendo encima de él y se quita la ropa*", 16);
                    RoleplayManager.Shout(TargetClient, "*Gime y tiembla porque " + Session.GetHabbo().Username + "'s mete los dedos dentro de ella*", 16);
                    TargetClient.SendWhisper("Gracias al sexo que tuviste tu felicidad está al máximo");
                    Session.SendWhisper("Gracias al sexo que tuviste tu felicidad está al máximo");
                }
                else if (Session.GetHabbo().Gender.ToLower().StartsWith("m") && TargetClient.GetHabbo().Gender.ToLower().StartsWith("m"))
                {
                    Session.Shout("*Agarra a " + TargetClient.GetHabbo().Username + " Por el pecho y lo abate, quitándose la ropa*", 16);
                    RoleplayManager.Shout(TargetClient, "*Gime del placer como " + Session.GetHabbo().Username + " Sube y baja encima de su pene*", 16);
                    TargetClient.SendWhisper("Gracias al sexo que tuviste tu felicidad está al máximo");
                    Session.SendWhisper("Gracias al sexo que tuviste tu felicidad está al máximo");
                }
                else if (Session.GetHabbo().Gender.ToLower().StartsWith("f") && TargetClient.GetHabbo().Gender.ToLower().StartsWith("f"))
                {
                    Session.Shout("*Empuja a " + TargetClient.GetHabbo().Username + " Abajo en el piso, subiendo encima de ella y mentidole rico*", 16);
                    RoleplayManager.Shout(TargetClient, "*Gime y tiembla como " + Session.GetHabbo().Username + "'s mete los dedos dentro de ella*", 16);
                    TargetClient.SendWhisper("Gracias al sexo que tuviste tu felicidad está al máximo");
                    Session.SendWhisper("Gracias al sexo que tuviste tu felicidad está al máximo");
                }
                else
                {
                    Session.Shout("*Hace el amor rico a " + TargetClient.GetHabbo().Username + "*", 16);
                    TargetClient.SendWhisper("Gracias al sexo que tuviste tu felicidad está al máximo");
                    Session.SendWhisper("Gracias al sexo que tuviste tu felicidad está al máximo");
                    RoleplayManager.Shout(TargetClient, "*Ay si, dame de lo tuyo " + Session.GetHabbo().Username + "'s ¡Muevete así!*", 16);
                }
                Session.GetRoleplay().CooldownManager.CreateCooldown("sex", 1000, 60);
                Session.GetRoleplay().CurEnergy -= 5;
                RoomUser.ApplyEffect(507);
                TargetUser.ApplyEffect(507);
                Session.GetRoleplay().Animo = 100;
                TargetClient.GetRoleplay().Animo = 100;
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