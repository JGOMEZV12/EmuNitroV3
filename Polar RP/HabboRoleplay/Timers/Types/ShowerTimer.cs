using System;
using System.Linq;
using System.Drawing;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;
using Polar.Core;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Begins showering
    /// </summary>
    public class ShowerTimer : RoleplayTimer
    {
        public ShowerTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {
            // Convert to milliseconds
            if (Client.GetRoleplay().Hygiene > 100)
                Client.GetRoleplay().Hygiene = 100;

            if (Client.GetRoleplay().Hygiene < 0)
                Client.GetRoleplay().Hygiene = 0;

            TimeLeft = (100 - Client.GetRoleplay().Hygiene + Client.GetRoleplay().Poop) * 1000;
        }

        /// <summary>
        /// Executes shower tick
        /// </summary>
        public override void Execute()
        {
            try
            {
                if (base.Client == null || base.Client.GetHabbo() == null || base.Client.GetRoleplay() == null)
                {
                    base.EndTimer();
                    return;
                }

                if (base.Client.GetRoomUser() == null || base.Client.GetRoomUser().GetRoom() == null)
                {
                    base.Client.GetRoleplay().InShower = false;
                    base.EndTimer();
                    return;
                }

                int ItemId = (int)Params[0];
                Item Shower = base.Client.GetRoomUser().GetRoom().GetRoomItemHandler().GetItem(ItemId);

                if (Shower == null || !base.Client.GetRoleplay().InShower || Shower.Coordinate != base.Client.GetRoomUser().Coordinate)
                {
                    RoleplayManager.Shout(base.Client, "Se detiene a tomar una ducha antes de que terminen*", 4);
                    base.Client.GetRoleplay().InShower = false;
                    base.EndTimer();
                    return;
                }

                TimeLeft -= 1000;
                base.Client.GetRoleplay().Hygiene++;

                if (TimeLeft > 0)
                    return;

                Point OffShower = new Point(Shower.SquareInFront.X, Shower.SquareInFront.Y);
                base.Client.GetRoomUser().MoveTo(OffShower);

                RoleplayManager.Shout(base.Client, "*Se siente limpi@ después de una buena ducha caliente*", 4);
                base.Client.GetRoleplay().InShower = false;
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