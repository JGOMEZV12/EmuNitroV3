using System;
using System.Linq;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using Polar.HabboRoleplay.Houses;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Apartment
{
    class RoomKickCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_house_kick_room"; }
        }

        public string Parameters
        {
            get { return "%message%"; }
        }

        public string Description
        {
            get { return "Expulsa a todos los que están dentro de tu casa enviandoles una razón."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            House House;
            if (!Room.TryGetHouse(out House))
            {
                Session.SendWhisper("No estás dentro de tu casa.", 1);
                return;
            }

            if (House.OwnerId != Session.GetHabbo().Id && !Session.GetHabbo().GetPermissions().HasRight("mod_tool"))
            {
                Session.SendWhisper("No eres el propietario de esta casa.", 1);
                return;
            }

            if (Params.Length == 1)
            {
                Session.SendWhisper("Por favor, proporciona la razón de esta acción. :expulsartodos [razón]", 1);
                return;
            }

            string Message = CommandManager.MergeParams(Params, 1);
            foreach (RoomUser RoomUser in Room.GetRoomUserManager().GetUserList().ToList())
            {
                if (RoomUser == null || RoomUser.IsBot || RoomUser.GetClient() == null || RoomUser.GetClient().GetHabbo() == null || RoomUser.GetClient().GetHabbo().GetPermissions().HasRight("mod_tool") || RoomUser.GetClient().GetHabbo().Id == Session.GetHabbo().Id)
                    continue;

                RoomUser.GetClient().SendNotification("Todos han sido expulsados de la casa por la siguiente razón: " + Message);

                //RoomData roomData = PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(House.Sign.RoomId);
                //RoleplayManager.SendUser(Session, roomData.Id);
            }

            Session.SendWhisper("Se han expulsado a todos de tu casa satisfactoriamente.", 1);
        }
    }
}
