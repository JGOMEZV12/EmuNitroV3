using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.Communication.Packets.Outgoing.Notifications;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Police
{
    class PoliceTrialCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_police_trial"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Permite que un usuario en prueba."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
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

            if (TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            if (!GroupManager.HasJobCommand(Session, "trial") && !Session.GetHabbo().GetPermissions().HasRight("give_police_trial"))
            {
                Session.SendWhisper("Sólo un jefe de policía puede usar este comando", 1);
                return;
            }

            if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("give_police_trial"))
            {
                Session.SendWhisper("¡Debes estar trabajando para usar este comando!", 1);
                return;
            }

            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("¡No puedes arrestar a alguien que no está jugando el juego ahora mismo!", 1);
                return;
            }

            int PoliceTrialRoom = Convert.ToInt32(RoleplayData.GetData("police", "trialroomid"));
            int PoliceTrialRoom2 = Convert.ToInt32(RoleplayData.GetData("police", "trialroomid2"));

            if (Room.Id != PoliceTrialRoom && Room.Id != PoliceTrialRoom2)
            {
                Session.SendWhisper("Lo siento, pero debes estar dentro de la sala de juicios de la policía", 1);
                return;
            }
            #endregion

            #region Execute
            if (TargetClient.GetRoleplay().PoliceTrial)
            {
                TargetClient.GetRoleplay().PoliceTrial = false;
                Session.Shout("*Elimina a " + TargetClient.GetHabbo().Username + " De su juicio policial*", 37);
            }
            else
            {
                TargetClient.GetRoleplay().PoliceTrial = true;
                Session.Shout("*Da un lugar a " + TargetClient.GetHabbo().Username + " En un juicio policial*", 37);
            }
            #endregion
        }
    }
}