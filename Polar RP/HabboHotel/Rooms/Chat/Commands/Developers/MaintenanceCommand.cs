using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Moderation;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Availability;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Developers
{
    class MaintenanceCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_maintenance"; }
        }

        public string Parameters
        {
            get { return "%minutes% %duration%"; }
        }

        public string Description
        {
            get { return "Put the hotel under maintenance for a specific amount of minutes and a specific amount of duration to when it is back."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {


            if (Params.Length < 3)
            {
                Session.SendWhisper("Oops, you must select minute(s) and duration.", 1);
                return;
            }

            
            int Minutes = Convert.ToInt32(Params[1]);
            int Duration = Convert.ToInt32(Params[2]);

            PolarEnvironment.GetGame().GetClientManager().SendMessage(new MaintenanceStatusComposer(Minutes, Duration));
            Session.SendWhisper("Success, the hotel will go down in " + Minutes + " minute(s) and the given duration to when the hotel is going to be back online is in " + Duration + " minute(s)!");
        }
    }
}
