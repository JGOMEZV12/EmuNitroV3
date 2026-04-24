using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;
using System.Drawing;
using Polar.Utilities;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Marriage
{
    class EmbarazarCommand : IChatCommand
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
                Session.SendWhisper("¡No tienes suficiente energía para tener sexo y tener un bebé ve a tomar una cerveza!", 1);
                return;
            }

            if (Session.GetRoleplay().MarriedTo != TargetClient.GetHabbo().Id)
            {
                Session.SendWhisper("¡Solo puedes tener un bebé con tu pareja!", 1);
                return;
            }

            if (Session.GetRoleplay().MarriedTo == 0)
            {
                Session.SendWhisper("¡Debes casarte para poder tener un embarazo!", 1);
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

            if (Session.GetHabbo().Gender.ToLower().StartsWith("f"))
            {
                Session.SendWhisper("¡Solo los hombres pueden embarazar!", 16);
                return;
            }

            if (TargetClient.GetHabbo().Gender.ToLower().StartsWith("m"))
            {
                Session.SendWhisper("Lo sentimos pero los hombres no pueden quedar embarazados, solo en peliculas", 16);
                return;
            }

            if (TargetClient.GetRoleplay().Embarazo > 1)
            {
                Session.SendWhisper("Lo sentimos pero tu pareja ya está esperando un bebé", 16);
                return;
            }

            if (TargetClient.GetRoleplay().Hijo > 0)
            {
                Session.SendWhisper("Lo sentimos, pero este usuario ya tiene hijo", 1);
                return;
            }
            #endregion

            #region Execute
            Point ClientPos = new Point(RoomUser.X, RoomUser.Y);
            Point TargetClientPos = new Point(TargetUser.X, TargetUser.Y);
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);
            CryptoRandom Random = new CryptoRandom();
            int Chance = Random.Next(1, 20);

            if (Chance <= 2)
            {
                Session.Shout("*Intenta embarazar a su pareja pero falla ¡Que lastima!*", 4);
                return;
            }

            if (Distance <= 1)
            {
                if (Session.GetHabbo().Gender.ToLower().StartsWith("m") && TargetClient.GetHabbo().Gender.ToLower().StartsWith("f"))
                {
                    Session.Shout("*Agarra a " + TargetClient.GetHabbo().Username + " Por el pecho y la aborda, quitándose la ropa*", 16);
                    RoleplayManager.Shout(TargetClient, "*Gime y tiembla porque " + Session.GetHabbo().Username + " Empuja su pene dentro de ella*", 16);
                    Session.SendWhisper("Gracias al sexo que tuviste tu felicidad está al máximo");
                    TargetClient.SendWhisper("¡Felicidades estás embarazada, ahora ve al hospital y escribe :hijo x (Al usuario que quieras solicitar que sea tu hij@)");
                    TargetClient.SendWhisper("Si te matan en las calles o en otro lugar, perderás el embarazo, así que cuidate mucho");
                    TargetClient.GetRoleplay().TimerManager.CreateTimer("embarazo", 1000, false);
                }
                Session.GetRoleplay().CooldownManager.CreateCooldown("sex", 1000, 60);
                Session.GetRoleplay().CurEnergy -= 5;
                RoomUser.ApplyEffect(507);
                TargetUser.ApplyEffect(507);
                Session.GetRoleplay().Animo = 100;
                TargetClient.GetRoleplay().Embarazo = 1;
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