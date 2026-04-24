using System;
using System.Collections.Generic;
using System.Linq;

using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Items;
using Polar.Core;
using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using System.Threading;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Countdown to nuke the RP
    /// </summary>
    public class BreakdownTimer : SystemRoleplayTimer
    {
        public BreakdownTimer(string Type, int Time, bool Forever, object[] Params) 
            : base(Type, Time, Forever, Params)
        {
            // 5 minutes converted to milliseconds
            int BreakdownTime = RoleplayManager.BreakDownMinutes;
            TimeLeft = BreakdownTime * 60000;
            TimeCount = 0;
        }

        /// <summary>
        /// Nuke process
        /// </summary>
        public override void Execute()
        {
            try
            {
                GameClient Nuker = (GameClient)Params[0];

                if (Nuker == null || Nuker.LoggingOut || Nuker.GetHabbo() == null || Nuker.GetRoleplay() == null || Nuker.GetRoleplay().IsDead || Nuker.GetRoleplay().IsJailed)
                {
                    base.EndTimer();
                    return;
                }

                if (!RoleplayManager.GenerateRoom(Nuker.GetHabbo().CurrentRoomId, out Room Room))
                    return;

                List<Item> Items = Room.GetGameMap().GetRoomItemForSquare(Nuker.GetRoomUser().Coordinate.X, Nuker.GetRoomUser().Coordinate.Y);

                if (Items.Count < 1)
                {
                    RoleplayManager.Shout(Nuker, "*Stops the nuking breakdown process*", 4);
                    base.EndTimer();
                    return;
                }

                bool HasCaptureTile = Items.ToList().Where(x => x.GetBaseItem().ItemName == "actionpoint01").ToList().Count() > 0;

                if (!HasCaptureTile)
                {
                    RoleplayManager.Shout(Nuker, "*Stops the nuking breakdown process*", 4);
                    base.EndTimer();
                    return;
                }

                TimeCount++;
                TimeLeft -= 1000;

                if (TimeLeft > 0)
                {
                    if (TimeCount == 60)
                    {
                        RoleplayManager.Shout(Nuker, "*Gets closer to complete the nuking breakdown process [" + (TimeLeft / 60000) + " Minutes Remaining]*", 4);
                        TimeCount = 0;
                    }
                    return;
                }

                // TODO: Execute the event after the timer is finished.

                base.EndTimer();
                return;
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error in Execute() void: " + e);
                base.EndTimer();
            }
        }
    }
}