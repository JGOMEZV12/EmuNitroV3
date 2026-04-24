using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Police
{
    class ClearWantedCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_police_clear_wanted"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Borra toda la lista de peligro"; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            int Bubble = 37;
            var RoomUser = Session.GetRoomUser();

            if (RoomUser == null)
                return;

            if (!GroupManager.HasJobCommand(Session, "clearwanted") && !Session.GetHabbo().GetPermissions().HasRight("roleplay_corp_manager"))
            {
                Session.SendWhisper("Sólo un jefe de policía puede usar este comando", 1);
                return;
            }

            if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("roleplay_corp_manager"))
            {
                Session.SendWhisper("¡Debes estar trabajando para usar este comando!", 1);
                return;
            }

            if (RoleplayManager.WantedList.Count <= 0)
            {
                Session.SendWhisper("¡La lista de buscados ya está vacía!", 1);
                return;
            }

            if (Session.GetHabbo().GetPermissions().HasRight("roleplay_corp_manager") && !Session.GetRoleplay().IsWorking)
                Bubble = 24;
            #endregion

            #region Execute
            Session.Shout("*Borra toda la lista de Wanted, quitando a cualquiera que aún estaba en ella*", Bubble);
            RoleplayManager.WantedList.Clear();
            #endregion
        }
    }
}