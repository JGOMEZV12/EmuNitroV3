using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Guides;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.General
{
    class StopWorkCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_corp_work_stop"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "¡Deja de trabajar si ya estás trabajando!"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (!Session.GetRoleplay().IsWorking)
            {
                Session.SendWhisper("¡No estás trabajando!", 1);
                return;
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes dejar de trabajar mientras estás muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes dejar de trabajar mientras estás preso", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("stopwork", true))
                return;

            #endregion

            if (GroupManager.HasJobCommand(Session, "guide"))
            {
                Session.GetRoleplay().IsWorking = false;

            }

            WorkManager.RemoveWorkerFromList(Session);
            Session.GetRoleplay().IsWorking = false;
            Session.Shout("*Piensa un poco y decide dejar de trabajar*", 4);
            HabboRoleplay.Misc.RoleplayManager.GetLookAndMotto(Session, "poof");
            Session.GetRoleplay().CooldownManager.CreateCooldown("stopwork", 1000, 10);
        }
    }
}