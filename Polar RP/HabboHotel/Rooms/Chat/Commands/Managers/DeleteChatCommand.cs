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
using Polar.Database.Interfaces;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Managers
{
    class DeleteChatCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_wonline"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Deletes a chat for abuse."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {

            #region Variables
            if (Params.Length < 2)
            {
                Session.SendWhisper("Invalid command syntax! :deletechat <chatname>", 1);
                return;
            }

            bool BanUserFromMaking = false;
            bool BanUserFromChatting = false;

            if (Params.Length >= 3)
            {
                if (Convert.ToString(Params[2]).ToLower().StartsWith("yes"))
                BanUserFromMaking = true;
            }

            if (Params.Length == 4)
            {
                if (Convert.ToString(Params[3]).ToLower().StartsWith("yes"))
                    BanUserFromChatting = true;
            }

            string ChatName = Convert.ToString(Params[1]);

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
                Session.SendWhisper("An error occured, this chat does not exist!", 1);
                return;
            }
            #endregion

            #region Execute          

            GameClient ChatOwner = null;

            if (Chat.ChatOwner > 0)
            {

               ChatOwner = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Chat.ChatOwner);

                using (IQueryAdapter DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    if (BanUserFromChatting)
                    {
                        if (ChatOwner == null)
                            DB.RunQuery("UPDATE rp_stats SET wchat_banned = '1' WHERE id = '" + Chat.ChatOwner + "'");
                        else
                        {
                            ChatOwner.GetRoleplay().BannedFromChatting = true;
                            ChatOwner.SendWhisper("You have been banned from ever being able to join a chat room");
                        }
                    }

                    if (BanUserFromMaking)
                    {
                        if (ChatOwner == null)
                            DB.RunQuery("UPDATE rp_stats SET wchat_making_banned = '1' WHERE id = '" + Chat.ChatOwner + "'");
                        else
                        {
                            ChatOwner.GetRoleplay().BannedFromMakingChat = true;
                            ChatOwner.SendWhisper("You have been banned from ever being able to make a chat room");
                        }
                    }
                }
            }
            else
            {
                if (BanUserFromChatting || BanUserFromMaking)
                Session.SendWhisper("There was no owner for this chat, thus there was nobody to ban!", 1);
            }

            Session.SendWhisper("Successfully deleted the chat '" + Chat.ChatName + "'. The owner has been notified!");

            if (ChatOwner != null)
            {
                ChatOwner.SendWhisper("Your chat '" + Chat.ChatName + "' has been deleted by a staff member!");
            }

            WebSocketChatManager.DeleteChat(Chat);

           #endregion

        }

    }
}
