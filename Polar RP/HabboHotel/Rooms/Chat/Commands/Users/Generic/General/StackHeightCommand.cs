using Polar.HabboHotel.GameClients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.General
{
    class StackHeightCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_admin_taxi"; }
        }

        public string Parameters
        {
            get { return "%valor%"; }
        }

        public string Description
        {
            get { return "StackHeight."; }
        }
        public async Task Execute(GameClient Session, Room Room, string[] Params)
        {
            if (!Room.CheckRights(Session, true))
                return;

            RoomUser user = Room.GetRoomUserManager().GetRoomUserByHabboId(Session.GetHabbo().Id);
            if (user == null)
                return;

            if (Params.Length == 1)
            {
                Session.SendWhisper("Escribe el valor.", 34);
                return;
            }
            else if (Params[1].Equals("clear", StringComparison.Ordinal) || Params[1].Equals("limpiar", StringComparison.Ordinal))
            {
                user.ConstruitZMode = false;
                user.ConstruitHeigth = 0;
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

            user.ConstruitZMode = true;
            user.ConstruitHeigth = value;
            Session.SendWhisper("Valor cambiado a: " + value, 34);
        }
    }
}
