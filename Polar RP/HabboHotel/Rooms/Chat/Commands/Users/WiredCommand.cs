using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Commands;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users
{
    class WiredCommand : IChatCommand
    {
        public string PermissionRequired => "command_room_wired";

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Ver wireds"; }
        }

        public async Task Execute(GameClient Session, Room Room, string[] Params)
        {
            if (Room == null)
            {
                Session.SendWhisper("You need to be inside a room to use :wired.", 34);
                return;
            }

            bool hasRights = Room.CheckRights(Session)
                          || Room.OwnerId == Session.GetHabbo().Id
                          || Session.GetHabbo().GetPermissions().HasRight("room_any_rights");

            if (!hasRights)
            {
                Session.SendWhisper("You need room rights to open the Wired Creator Tools.", 34);
                return;
            }

            Session.SendMessage(new InClientLinkComposer("wired-tools/show"));
        }
    }
}