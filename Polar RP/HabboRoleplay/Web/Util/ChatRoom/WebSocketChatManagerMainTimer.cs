using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.Core;
using System.Linq;
using System.Collections.Generic;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboRoleplay.Farming;
using Polar.HabboRoleplay.Timers;

namespace Polar.HabboRoleplay.Web.Util.ChatRoom
{
    /// <summary>
    /// Check if farmingspaces have expired
    /// </summary>
    public class WebSocketChatManagerMainTimer : SystemRoleplayTimer
    {
        public WebSocketChatManagerMainTimer(string Type, int Time, bool Forever, object[] Params)
            : base(Type, Time, Forever, Params)
        {
            TimeCount = 0;
        }

        /// <summary>
        /// Checks the flooded user timers
        /// </summary>
        public override void Execute()
        {
            try
            {
                if (WebSocketChatManager.RunningChatRooms.Count <= 0)
                    return;

                foreach (WebSocketChatRoom ChatRoom in WebSocketChatManager.RunningChatRooms.Values)
                {

                    if (ChatRoom == null)
                        continue;

                    foreach(GameClient User in ChatRoom.ChatUsers.Keys)
                    {
                        if (User == null)
                            continue;

                        if (User.LoggingOut)
                            continue;

                        if (User.GetRoleplay() == null)
                            continue;

                        if (User.GetRoleplay().socketChatSpamTicks >= 0)
                        {
                            User.GetRoleplay().socketChatSpamTicks--;

                            if (User.GetRoleplay().socketChatSpamTicks == -1)
                            {
                                User.GetRoleplay().socketChatSpamCount = 0;
                            }
                        }

                      
                        if (User.GetRoleplay().socketChatFloodTime > 0)
                        {
                            User.GetRoleplay().socketChatFloodTime--;
                        }

                        if (User.GetRoleplay().socketChatSpamCount > 0)
                        {
                            User.GetRoleplay().socketChatSpamCount--;
                        }
                    }
                }


            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error in Execute() void of WebSocketChatManagerMainTimer: " + e);
                base.EndTimer();
            }
        }
    }
}