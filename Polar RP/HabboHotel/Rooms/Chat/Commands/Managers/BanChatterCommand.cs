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
    class BanChatterCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_websocket_chat_ban_chatter"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Prohíbe que un usuario se una a websocket chats."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {

            #region Params
            if (Params.Length < 2)
            {
                Session.SendWhisper("Sintaxis de comandos no válido! :banchatter <user>", 1);
                return;
            }

            string Chatter = Params[1].ToString();
            GameClient TargetSession = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Chatter);

            bool BanFromMaking = false;

            if (Params.Length == 3)
            {
                if (Convert.ToString(Params[2]).ToLower().StartsWith("yes"))
                    BanFromMaking = true;
            }
            #endregion

            #region Conditions
            if (TargetSession == null)
            {
                Session.SendWhisper("Objetivo no encontrado!", 1);
                return;
            }
            #endregion

            #region Execute

            if (TargetSession.GetRoleplay().ChatRooms.Count > 0)
            {
                foreach (WebSocketChatRoom Chat in TargetSession.GetRoleplay().ChatRooms.Values)
                {
                    if (Chat == null)
                        continue;

                    WebSocketChatManager.Disconnect(TargetSession, Chat.ChatName);
                }
            }

            Session.Shout("*Utiliza sus poderes divinos para prohibir a " + TargetSession.GetHabbo().Username + " de unirse a chat de salas" + ((BanFromMaking) ? ",O de hacer salas de chat" : "") + "!*", 23);
            TargetSession.SendWhisper("Usted ha sido prohibido de unirse a una sala de chat por un miembro del personal!", 1);
            TargetSession.GetRoleplay().BannedFromChatting = true;

            if (BanFromMaking)
            {
                TargetSession.SendWhisper("Usted ha sido prohibido de alguna vez hacer una sala de chat por un miembro del personal!", 1);
                TargetSession.GetRoleplay().BannedFromMakingChat = true;
            }

            //RoleplayManager.Shout(TargetSession, "*Agarra su " + RoleplayManager.GetPhoneName(TargetSession) + " y elimina su aplicación de WhatsApp*", 4);
           
            #endregion
        }
    }
}
