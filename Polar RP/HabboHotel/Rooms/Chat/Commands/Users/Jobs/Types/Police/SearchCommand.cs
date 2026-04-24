using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Police
{
    class SearchCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_police_search"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Registrar al ciudadano para ver si tienen alguna droga."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Vaya, te olvidaste de introducir un nombre de usuario es:  :revisar usuario", 1);
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

            if (!GroupManager.HasJobCommand(Session, "search"))
            {
                Session.SendWhisper("Sólo un oficial de policía puede utilizar este comando", 1);
                return;
            }

            if (!Session.GetRoleplay().IsWorking)
            {
                Session.SendWhisper("¡Debes estar trabajando para usar este comando!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes buscar a alguien que está muerto!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes buscar a alguien que está en la cárcel!", 1);
                return;
            }

            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("Usted no puede buscar a alguien que no está jugando el juego ahora mismo.", 1);
                return;
            }
            #endregion

            #region Execute
            Point ClientPos = new Point(RoomUser.X, RoomUser.Y);
            Point TargetClientPos = new Point(TargetUser.X, TargetUser.Y);
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

            if (Distance <= 1)
            {
                Random Random = new Random();

                int Chance = Random.Next(1, 101);

                if (Chance <= 8)
                {
                    Session.Shout("*Revisa a " + TargetClient.GetHabbo().Username + " Tratando de encontrar alguna droga, pero parece que no puede encontrar ninguna*", 37);
                    return;
                }
                else
                {
                    bool HasWeed = TargetClient.GetRoleplay().Weed > 0;
                    bool HasCocaine = TargetClient.GetRoleplay().Cocaine > 0;
                    bool HasHeroine = TargetClient.GetRoleplay().Heroina > 0;

                    if (!HasWeed && !HasCocaine && !HasHeroine)
                    {
                        Session.Shout("*Revisa a " + TargetClient.GetHabbo().Username + " Tratando de encontrar alguna droga, pero parece que no puede encontrar ninguna*", 37);
                        return;
                    }
                    else if (HasWeed && !HasCocaine && !HasHeroine)
                    {
                        Session.Shout("*Revisa a " + TargetClient.GetHabbo().Username + " y encuentra " + String.Format("{0:N0}", TargetClient.GetRoleplay().Weed) + "g de marihuana [LEGAL SON: 10g]*", 37);
                        return;
                    }
                    else if (HasCocaine && !HasWeed && !HasHeroine)
                    {
                        Session.Shout("*Revisa a " + TargetClient.GetHabbo().Username + " y encuentra " + String.Format("{0:N0}", TargetClient.GetRoleplay().Cocaine) + "g de cocaina [LEGAL SON: 8g] *", 37);
                        return;
                    }
                    else if (HasHeroine && !HasCocaine && !HasWeed)
                    {
                        Session.Shout("*Revisa a " + TargetClient.GetHabbo().Username + " y encuentra " + String.Format("{0:N0}", TargetClient.GetRoleplay().Heroina) + "g de heroina [LEGAL SON: 20g] *", 37);
                        return;
                    }
                    else
                    {
                        Session.Shout("*Revisa a " + TargetClient.GetHabbo().Username + " y encuentra " + String.Format("{0:N0}", TargetClient.GetRoleplay().Cocaine) + "g de cocaina [LEGAL SON: 8g] y " + String.Format("{0:N0}", TargetClient.GetRoleplay().Weed) + "g de marihuana [LEGAL SON: 10g]*", 37);
                        return;
                    }
                }
            }
            else
            {
                Session.SendWhisper("¡Debes acercarte a este ciudadano para revisarl!", 1);
                return;
            }
            #endregion
        }
    }
}