using System.Collections.Generic;
using Polar.HabboHotel.Groups;
using Polar.Communication.Packets.Outgoing.Groups;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Gangs
{
    class GangCreateCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_gang_create"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Abre la ventana de creación de pandillas."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            var Gang = GroupManager.GetGang(Session.GetRoleplay().GangId);

            if (Gang != null)
            {
                if (Gang.Id > 0)
                {
                    if (Gang.CreatorId == Session.GetHabbo().Id)
                    {
                        Session.SendWhisper("Por favor borre a su pandilla antes de intentar hacer una nueva!", 1);
                        return;
                    }
                }
            }

            List<RoomData> ValidRooms = new List<RoomData>();
            foreach (RoomData Data in Session.GetHabbo().UsersRooms)
            {
                if (Data.Group == null)
                    ValidRooms.Add(Data);
            }

            Session.SendMessage(new GroupCreationWindowComposer(ValidRooms));
        }
    }
}