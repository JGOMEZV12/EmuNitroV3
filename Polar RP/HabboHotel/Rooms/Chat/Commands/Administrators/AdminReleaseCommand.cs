using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Administrators
{
    class AdminReleaseCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_admin_release"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Libera al ciudadano de la cárcel si está encarcelado."; }
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
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez están sin conexión.", 1);
                return;
            }

            var RoomUser = Session.GetRoomUser();
            var TargetRoomUser = TargetClient.GetRoomUser();

            if (RoomUser == null || TargetRoomUser == null)
                return;

            if (!TargetClient.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("No puedes liberar a alguien que no esté en la cárcel!", 1);
                return;
            }
            #endregion

            #region Execute

            Session.Shout("*Utiliza sus poderes divinos para liberar a " + TargetClient.GetHabbo().Username + " de la cárcel*", 23);
            TargetClient.GetRoleplay().IsJailed = false;
            TargetClient.GetRoleplay().JailedTimeLeft = -5;

            #endregion
        }
    }
}