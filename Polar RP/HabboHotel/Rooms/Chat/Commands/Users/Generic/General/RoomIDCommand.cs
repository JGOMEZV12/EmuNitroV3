using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.General
{
    class RoomIDCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_general_room_id"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Sirve para saber en donde te encuentras."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Room == null)
            {
                Session.SendWhisper("Por alguna extraña razón, los datos de esta habitación no se pudo encontrar!", 1);
                return;
            }

            Session.SendWhisper("Actualmente te encuentras en la sala: " + Room.Id + "!", 34);
            return;
        }
    }
}