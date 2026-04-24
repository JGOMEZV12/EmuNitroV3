using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.Core;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Countdown for probation
    /// </summary>
    public class ProbationTimer : RoleplayTimer
    {
        public ProbationTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {
            TimeLeft = base.Client.GetRoleplay().ProbationTimeLeft * 60000;
        }

        /// <summary>
        /// Removes the user from probation
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

                if (!base.Client.GetRoleplay().OnProbation)
                {
                    base.Client.GetRoleplay().OnProbation = false;
                    base.Client.GetRoleplay().ProbationTimeLeft = 0;
                    base.EndTimer();
                    return;
                }

                if (base.Client.GetRoomUser() == null)
                    return;

                TimeCount++;
                TimeLeft -= 1000;

                if (TimeCount == 60)
                    base.Client.GetRoleplay().ProbationTimeLeft--;

                if (TimeLeft > 0)
                {
                    if (TimeCount == 60)
                    {
                        base.Client.SendWhisper("Tu tienes " + (TimeLeft / 60000) + " Minutos, hasta que se retire de la libertad condicional", 1);
                        TimeCount = 0;
                    }
                    return;
                }

                base.Client.GetRoleplay().OnProbation = false;
                base.Client.GetRoleplay().ProbationTimeLeft = 0;
                RoleplayManager.Shout(base.Client, "*Completa su tiempo de prueba y se retira de la libertad condicional*", 4);
                base.EndTimer();
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error in Execute() void: " + e);
                base.EndTimer();
            }
        }
    }
}