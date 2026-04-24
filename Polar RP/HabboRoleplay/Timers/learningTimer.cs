using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.Core;

namespace Polar.HabboRoleplay.Timers
{
    public class learningTimer : RoleplayTimer
    {
        int timeLeft; // 15 minutes (milliseconds)

        public learningTimer(string Type, GameClient Session, int Time, bool Forever, object[] Params)
            : base(Type, Session, Time, Forever, Params)
        {

            timeLeft = 180 * 1000;
        }

        public override void Execute()
        {
            try
            {
                if (base.Client == null || base.Client.GetHabbo() == null || base.Client.GetRoleplay() == null)
                {
                    base.EndTimer();
                    return;
                }

                if (base.Client.GetHabbo().CurrentRoom == null)
                    return;

                if (base.Client.GetHabbo().CurrentRoomId <= 0)
                    return;

                TimeCount++;
                TimeLeft -= 1000;

                #region Conditions

                if (timeLeft > 0)
                {
                    if (TimeCount == 60)
                    {
                        int minutesRemaining = timeLeft / 60000;
                        base.Client.SendWhisper("Tienes " + minutesRemaining + " minuto(s) Para que aprendas nuevas cosas.");
                        TimeCount = 0;
                    }
                    return;
                }

                #endregion

                #region Execute

                LevelManager.AddIntelligenceEXP(base.Client, 1);
                RoleplayManager.Shout(base.Client, "*Termino de leer [+1] Inteligencia ¡Wooow!*");
                Client.SendWhisper("*Tu inteligencia es ahora: " + Client.GetRoleplay().Intelligence + "*");
                base.Client.GetRoleplay().Learning = false;

                base.EndTimer();
                #endregion
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error in Execute() void: " + e);
                base.EndTimer();
            }
        }
    }
}
