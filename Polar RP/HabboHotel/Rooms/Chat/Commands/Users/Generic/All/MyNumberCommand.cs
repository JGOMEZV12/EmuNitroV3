using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.HabboRoleplay.RPRoom;
using Polar.Database.Interfaces;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Police
{
    class MyNumberCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_my_number"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Mira tu número telefónico."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Session.GetRoleplay().Phone == 0)
            {
                Session.SendWhisper("No tienes ningún teléfono comprado para hacer eso.", 1);
                return;
            }

            Session.SendWhisper("Número Telefónico: " + Session.GetRoleplay().PhoneNumber, 1);
        }
    }
}