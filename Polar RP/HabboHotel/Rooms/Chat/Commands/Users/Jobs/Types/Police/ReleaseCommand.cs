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
    class ReleaseCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_police_release"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Libera a un convicto de la cárcel."; }
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

            if (!GroupManager.HasJobCommand(Session, "release"))
            {
                Session.SendWhisper("Sólo un oficial de policía puede utilizar este comando", 1);
                return;
            }

            if (!Session.GetRoleplay().IsWorking)
            {
                Session.SendWhisper("¡Debes estar trabajando para usar este comando!", 1);
                return;
            }

            if (!TargetClient.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes liberar a alguien que ya no está en la cárcel!", 1);
                return;
            }

            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("¡No puedes liberar a alguien que no está jugando el juego ahora mismo!", 1);
                return;
            }

            if (TargetClient.GetRoomUser().RoomId != Session.GetRoomUser().RoomId)
            {
                Session.SendWhisper(TargetClient.GetHabbo().Username + " ¡Ni siquiera está en la misma habitación que tú!", 1);
                return;
            }
            #endregion

            #region Execute
            Session.Shout("*Libera a " + TargetClient.GetHabbo().Username + " De la cárcel en libertad condicional*", 37);
            TargetClient.GetRoleplay().IsJailed = false;
            TargetClient.GetRoleplay().IsStun = false;
            TargetClient.GetRoleplay().Paralized = false;
            TargetClient.GetRoleplay().JailedTimeLeft = 0;
            #endregion
        }
    }
}