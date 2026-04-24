using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Bots;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Start work timer
    /// </summary>
    public class StartWorkTimer : BotRoleplayTimer
    {
        public StartWorkTimer(string Type, RoleplayBot CachedBot, int Time, bool Forever, object[] Params) 
            : base(Type, CachedBot, Time, Forever, Params)
        {
            TimeCount = 0;
        }
 
        /// <summary>
        /// starts work
        /// </summary>
        public override void Execute()
        {
            try
            {
                if (base.CachedBot == null || base.CachedBot.DRoomUser == null || base.CachedBot.DRoom == null)
                {
                    base.EndTimer();
                    return;
                }

                var Point = new System.Drawing.Point(base.CachedBot.DRoomUser.GetBotRoleplay().oX, base.CachedBot.DRoomUser.GetBotRoleplay().oY);

                if (base.CachedBot.DRoomUser.Coordinate != Point)
                    return;

                base.EndTimer();
            }
            catch
            {
                base.EndTimer();
            }
        }
    }
}