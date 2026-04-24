using System;
using System.Threading;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.HabboRoleplay.Houses;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Apartment
{
    class KickCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_house_kick"; }
        }

        public string Parameters
        {
            get { return "%username% %reason%"; }
        }

        public string Description
        {
            get { return "Expulsa a un usuario de tu habitación enviandole una razón."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            House House;
            if (!Room.TryGetHouse(out House))
            {
                Session.SendWhisper("No te encuentras dentro de tu casa.", 1);
                return;
            }

            if (House.OwnerId != Session.GetHabbo().Id && !Session.GetHabbo().GetPermissions().HasRight("mod_tool"))
            {
                Session.SendWhisper("No eres propietario de esta casa.", 1);
                return;
            }

            if (Params.Length == 1)
            {
                Session.SendWhisper("Debes ingresar el nombre de la persona a expulsar.", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null || TargetClient.GetRoomUser() == null)
            {
                Session.SendWhisper("Ha ocurrido un error en buscar a esa persona, probablemente esté desconectada.", 1);
                return;
            }

            if (TargetClient.GetRoomUser().RoomId != Room.RoomId)
            {
                Session.SendWhisper("Esa persona no está en tu casa.", 1);
                return;
            }

            if (TargetClient.GetHabbo().Username == Session.GetHabbo().Username)
            {
                Session.SendWhisper("No puedes expulsarte a ti mism@.", 1);
                return;
            }

            Room TargetRoom;
            if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(TargetClient.GetHabbo().CurrentRoomId, out TargetRoom))
                return;

            if (Params.Length > 2)
                TargetClient.SendNotification("Has sido expulsado de la casa por la siguiente razón: " + CommandManager.MergeParams(Params, 2));
            else
                TargetClient.SendNotification("Has sido expulsdo de la casa.");
            /*
            if (TargetClient.GetRoomUser() != null)
            {
                if (TargetClient.GetRoomUser().RoomId == House.Sign.RoomId)
                   return;
            }
            

            RoomData roomData = PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(House.Sign.RoomId);
            RoleplayManager.SendUser(TargetClient, roomData.Id); */
        }
    }
}
