using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using System.Threading.Tasks;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Administrators
{
    internal class EventAlertCommand : IChatCommand
    {
        public string PermissionRequired => "command_hotel_alert_event";
        public string Parameters => "%mensaje%";
        public string Description => "¡Envía una alerta de hotel para tu evento!";

        public async Task Execute(GameClient session, Room room, string[] Params)
        {
            if (Params.Length == 1)
            {
                session.SendWhisper("¡Por favor, ingrese un mensaje de premio!", 1);
                return;
            }

            string message = CommandManager.MergeParams(Params, 1);
            PolarEnvironment.GetGame().GetClientManager().SendMessage(new RoomEventNotificationComposer(session, "event", message));
            return;
        }
    }
}
