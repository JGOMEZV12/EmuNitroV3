using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.SpecialRights
{
    class ColourChangeCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_colour_change"; }
        }

        public string Parameters
        {
            get { return "%colour%"; }
        }

        public string Description
        {
            get { return "Allows you to change your names colour."; }
        }

        public async Task Execute(GameClients.GameClient Session, Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Tienes que colocar el codigo del color ejemplo :color rainbow/yellow/red/blue/green", 1);
                return;
            }

            if (Session.GetHabbo() == null)
                return;

            if (Params[1].ToLower() == "remove")
            {
                Session.GetHabbo().NameColor = string.Empty;
                UpdateDatabase(Session);
            }
            else
            {
                Session.GetHabbo().NameColor = Params[1];
                UpdateDatabase(Session);
                Session.GetRoomUser().SendNameColourPacket();
            }

            Session.SendWhisper("¡Has cambiado tu código de color con éxito!", 1);
            return;
        }

        public void UpdateDatabase(GameClients.GameClient Session)
        {
            if (Session == null || Session.GetHabbo() == null)
                return;

            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("UPDATE `users` SET `colour` = '" + Session.GetHabbo().NameColor + "' WHERE `id` = '" + Session.GetHabbo().Id + "' LIMIT 1");
            }
        }
    }
}