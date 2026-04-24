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
    class SetSHCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_setz"; }
        }

        public string Parameters
        {
            get { return "%height%"; }
        }

        public string Description
        {
            get { return "Establezca una altura para que los muebles sean apilados."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Escribe el valor.", 34);
                return;
            }
            else if (Params[1].Equals("clear", StringComparison.Ordinal) || Params[1].Equals("limpiar", StringComparison.Ordinal))
            {
                Session.GetHabbo().DebugStacking = false;
                Session.GetHabbo().StackHeight = 0;
                Session.SendWhisper("Comando deshabilitado.", 34);
                return;
            }

            double value;
            if (!double.TryParse(Params[1], out value))
                return;

            if (value < 0 || value > 100)
            {
                Session.SendWhisper("Entre 1 y 100.", 34);
                return;
            }

            Session.GetHabbo().DebugStacking = true;
            Session.GetHabbo().StackHeight = value;
            Session.SendWhisper("La altura de la pila cambió a: " + value + "", 1);
            return;
        }
    }
}