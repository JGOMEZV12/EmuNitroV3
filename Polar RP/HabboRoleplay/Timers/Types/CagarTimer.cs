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
    /// Begins Pooping
    /// </summary>
    public class CagarTimer : RoleplayTimer
    {
        public CagarTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {
            // Convert to milliseconds
            if (Client.GetRoleplay().Poop > 100)
                Client.GetRoleplay().Poop = 100;

            if (Client.GetRoleplay().Poop < 0)
                Client.GetRoleplay().Poop = 0;

            TimeLeft = (100 - Client.GetRoleplay().Poop) * 1000;
        }

        /// <summary>
        /// Executes Poop tick
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
                    base.Client.GetRoleplay().InCagar = false;
                    base.EndTimer();
                    return;
                }

                int ItemId = (int)Params[0];
                Item Cagar = base.Client.GetRoomUser().GetRoom().GetRoomItemHandler().GetItem(ItemId);

                if (Cagar == null || !base.Client.GetRoleplay().InCagar || Cagar.Coordinate != base.Client.GetRoomUser().Coordinate)
                {
                    RoleplayManager.Shout(base.Client, "*Se levanta del toilet antes de terminar y no se limpia ¡ASCO!*", 4);
                    base.Client.GetRoleplay().InCagar = false;
                    base.EndTimer();
                    return;
                }

                TimeLeft -= 1000;
                base.Client.GetRoleplay().Poop++;

                if (TimeLeft > 0)
                    return;

                Point OffCagar = new Point(Cagar.SquareInFront.X, Cagar.SquareInFront.Y);
                base.Client.GetRoomUser().MoveTo(OffCagar);

                RoleplayManager.Shout(base.Client, "*Apreta el botón y baja toda la mierda, se siente liviano*", 4);
                base.Client.GetRoleplay().InCagar = false;
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