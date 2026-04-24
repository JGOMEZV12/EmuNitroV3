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

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Check if day and night is operating
    /// </summary>
    public class DayNightCycleTimer : SystemRoleplayTimer
    {
        public DayNightCycleTimer(string Type, int Time, bool Forever, object[] Params) 
            : base(Type, Time, Forever, Params)
        {
            TimeCount = 0;
        }
 
        /// <summary>
        /// Executes the day and night process
        /// </summary>
        public override void Execute()
        {
            try
            {
                if (PolarEnvironment.GetGame() == null)
                    return;

                if (PolarEnvironment.GetGame().GetRoomManager() == null)
                    return;

                if (PolarEnvironment.GetGame().GetRoomManager().GetRooms().Count <= 0)
                    return;

                DayNightManager.SetTime();
            }
            catch(Exception e)
            {
                Logging.LogRPTimersError("Error in Execute() void: " + e);
                base.EndTimer();
            }
        }
    }
}