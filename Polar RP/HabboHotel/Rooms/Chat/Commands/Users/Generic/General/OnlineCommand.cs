using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.General
{
    class OnlineCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_general_whos_online"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Le dice a todos los ciudadanos actuales en línea."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            StringBuilder Online = new StringBuilder();

            var Users = PolarEnvironment.GetGame().GetClientManager().GetClients.Where(x => x != null && x.GetHabbo() != null && !x.GetHabbo().AppearOffline).ToList();
            var HiddenUsers = PolarEnvironment.GetGame().GetClientManager().GetClients.Where(x => x != null && x.GetHabbo() != null && x.GetHabbo().AppearOffline).ToList();

            lock (Users)
            {
                Online.Append("Hay  " + String.Format("{0:N0}", Users.Count) + " ciudadanos conectados (" + String.Format("{0:N0}", HiddenUsers.Count) + " Usuarios en línea): \n");

                foreach (var client in Users)
                {
                    if (client == null || client.GetHabbo() == null)
                        continue;

                    if (Session.GetHabbo().GetPermissions().HasRight("mod_tool"))
                        Online.Append("- " + client.GetHabbo().Username + "\n");
                    else
                        Online.Append("- " + client.GetHabbo().Username + "\n");
                }
            }

            if (Session.GetHabbo().GetPermissions().HasRight("mod_tool"))
            {
                lock (HiddenUsers)
                {
                    foreach (var client in HiddenUsers)
                    {
                        if (client == null || client.GetHabbo() == null)
                            continue;

                        if (Session.GetHabbo().GetPermissions().HasRight("mod_tool"))
                            Online.Append("- " + client.GetHabbo().Username + "\n");
                        else
                            Online.Append("- " + client.GetHabbo().Username + "\n");
                    }
                }
            }

            Session.SendMessage(new MOTDNotificationComposer(Online.ToString()));
        }
    }
}