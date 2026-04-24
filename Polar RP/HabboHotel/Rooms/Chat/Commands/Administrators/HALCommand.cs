using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using System.Threading.Tasks;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Administrators
{
    class HALCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_hotel_alert_link"; }
        }

        public string Parameters
        {
            get { return "%message%"; }
        }

        public string Description
        {
            get { return "Send a message to the entire hotel, with a link."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 2)
            {
                Session.SendWhisper("Please enter a message and a URL to send.", 1);
                return;
            }

            string URL = Params[1];

            string Message = CommandManager.MergeParams(Params, 2);
            PolarEnvironment.GetGame().GetClientManager().SendMessage(new RoomNotificationComposer("Hotel Alert!", Message + "\r\n" + "- " + Session.GetHabbo().Username, "", "Click Here!", URL));
            return;
        }
    }
}
