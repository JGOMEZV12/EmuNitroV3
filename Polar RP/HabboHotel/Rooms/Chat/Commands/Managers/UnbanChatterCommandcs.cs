using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Fleck;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboRoleplay.Web.Util.ChatRoom;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Managers
{
    class UnBanChatterCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_websocket_chat_unban_chatter"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Desbanea a un usuario de los websocket chats."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {

            if (Params.Length < 2)
            {
                Session.SendWhisper("Sintaxis de comandos no válido! :banchatter <user>", 1);
                return;
            }

            bool UnbanFromMakingChats = false;

            if (Params.Length > 2)
            {
                if (Convert.ToString(Params[2]).ToLower().StartsWith("yes"))
                    UnbanFromMakingChats = true;
            }

            string Chatter = Params[1].ToString();
            GameClient TargetSession = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Chatter);

            if (TargetSession == null)
            {
                Session.SendWhisper("Objetivo no encontrado!", 1);
                return;
            }

            Session.Shout("*Utiliza sus poderes divinos para desbanear a " + TargetSession.GetHabbo().Username + " de las salas de chat websocket" + ((UnbanFromMakingChats) ? ", y de hacer nuevos chat" : "") + "!*", 23);

            TargetSession.GetRoleplay().BannedFromChatting = false;
            
            if (UnbanFromMakingChats)
            {
                TargetSession.GetRoleplay().BannedFromMakingChat = false;
            }
        }
    }
}
