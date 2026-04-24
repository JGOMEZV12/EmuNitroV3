using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Fleck;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboRoleplay.Web.Util.ChatRoom;
using Newtonsoft.Json;
using Polar.HabboHotel.Rooms.Games;
using Polar.HabboHotel.Groups;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Managers
{
    class TLockCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_websocket_chat_lock"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Locks a chat."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {

            #region Variables
            if (Params.Length < 2)
            {
                Session.SendWhisper("Invalid command syntax! :togglechatlock <chatname>", 1);
                return;
            }

            string ChatName = Convert.ToString(Params[2]);
            #endregion

            #region Conditions

            if (!WebSocketChatManager.RunningChatRooms.ContainsKey(ChatName.ToLower()))
            {
                Session.SendWhisper("This chat '" + ChatName.ToLower() + "' does not exist!", 1);
                return;
            }

            WebSocketChatRoom Chat = WebSocketChatManager.RunningChatRooms[ChatName.ToLower()];

            if (Chat == null)
            {
                Session.SendWhisper("An error occurred, this chat does not exist!", 1);
                return;
            }

            if (Chat.ChatOwner != Session.GetHabbo().Id && Session.GetHabbo().VIPRank <= 1)
            {
                Session.SendWhisper("You must be the owner of this chat to do this!", 1);
                return;
            }

            #endregion

            #region Execute          

            bool CurrentLocked = Convert.ToBoolean(Chat.ChatValues["locked"]);

            if (CurrentLocked)
            {
                Chat.ChatValues["locked"] = false;
                Session.SendWhisper("Successfully unlocked the chat '" + Chat.ChatName + "'", 1);
            }
            else
            {
                Chat.ChatValues["locked"] = true;
                Session.SendWhisper("Successfully LOCKED the chat '" + Chat.ChatName + "'", 1);
            }

            #endregion

        }

    }
}
