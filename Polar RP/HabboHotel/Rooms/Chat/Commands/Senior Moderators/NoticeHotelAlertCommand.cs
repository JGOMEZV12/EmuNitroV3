using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Moderation;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators.Seniors
{
    class NoticeHotelAlertCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_hotel_alert_notice"; }
        }

        public string Parameters
        {
            get { return "%message%"; }
        }

        public string Description
        {
            get { return "Send a message to the entire hotel via whisper."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Please enter a message to send.", 1);
                return;
            }

            string Message = CommandManager.MergeParams(Params, 1);

            lock (PolarEnvironment.GetGame().GetClientManager().GetClients)
            {
                foreach (var client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                {
                    if (client == null || client.GetHabbo() == null)
                        continue;

                    client.SendMessage(new RoomNotificationComposer("staff_notice", "message", "Hotel Alert! " + Message + " - " + Session.GetHabbo().Username));
                }
            }
        }
    }
}
