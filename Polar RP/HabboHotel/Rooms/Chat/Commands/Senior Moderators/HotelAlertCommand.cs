using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Moderation;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators.Seniors
{
    class HotelAlertCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_hotel_alert"; }
        }

        public string Parameters
        {
            get { return "%message%"; }
        }

        public string Description
        {
            get { return "Send a message to the entire hotel."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Envía un mensaje a toda la ciudad.", 1);
                return;
            }

            string Message = CommandManager.MergeParams(Params, 1);

           PolarEnvironment.GetGame().GetClientManager().SendMessage(new BroadcastMessageAlertComposer(Message + "\r\n\n" + "Comunicado de: " + Session.GetHabbo().Username + "\r\n\n" + "CEO, de " + PolarEnvironment.GetConfig().data["hotel.name"]));
        }
    }
}
