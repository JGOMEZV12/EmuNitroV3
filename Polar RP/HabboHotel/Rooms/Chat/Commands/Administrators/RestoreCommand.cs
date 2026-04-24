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

namespace Polar.HabboHotel.Rooms.Chat.Commands.Administrators
{
    class RestoreCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_admin_restore"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Libera al ciudadano del hospital si está muerto."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Vaya, olvidó introducir un nombre de usuario!", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez están fuera de línea.", 1);
                return;
            }

            if (!TargetClient.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡Esa persona no se encuentra herida!", 1);
                return;
            }

            var RoomUser = Session.GetRoomUser();
            var TargetRoomUser = TargetClient.GetRoomUser();

            if (RoomUser == null || TargetRoomUser == null)
                return;
            #endregion

            #region Execute

            Session.Shout("*Utiliza sus poderes divinos para restaurar a " + TargetClient.GetHabbo().Username + ", sanandolo*", 23);
            TargetClient.GetRoleplay().IsDead = false;
            TargetClient.GetRoomUser().ApplyEffect(0);
            TargetClient.GetRoleplay().DeadTimeLeft = 0;
            TargetClient.GetRoleplay().CurHealth = TargetClient.GetRoleplay().MaxHealth;
            TargetClient.SendWhisper("Un administrador te ha restaurado!", 1);

            #endregion
        }
    }
}