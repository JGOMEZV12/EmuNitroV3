using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.VIP
{
    class StopSHCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_set_stack_height_undo"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Desactiva la altura de la pila de depuración de :setsh."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            Session.GetHabbo().DebugStacking = false;
            Session.GetHabbo().StackHeight = 0;
            Session.SendWhisper("Ha desactivado el apilado de depuración", 1);
            return;
        }
    }
}