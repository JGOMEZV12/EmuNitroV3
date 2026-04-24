using System;
using System.Linq;
using System.Text;
using System.Drawing;
using Polar.Utilities;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Criminal
{
    class BusinfoCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_criminal_activity_tutorial"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Proporciona información de bus de "+ PolarEnvironment.GetConfig().data["hotel.name"]+"."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            Session.SendMessage(new InternalLinkComposer("habbopages/roleplay/businfo.txt?" + UnixTimestamp.GetNow()));
        }
    }
}