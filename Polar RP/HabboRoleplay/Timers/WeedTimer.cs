using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboRoleplay.Timers
{
    public class WeedTimer
    {
        Timer timer;
        GameClient Session;
        int timeLeft; // 5 minutes (milliseconds)

        public WeedTimer(GameClient Session)
        {
            this.Session = Session;

            int time = 8;
            timeLeft = time * 10000;

            startTimer();
        }

        public void startTimer()
        {
            TimerCallback timerFinished = timerDone;

            timer = new Timer(timerFinished, null, 10000, Timeout.Infinite);
        }

        public void timerDone(object info)
        {
            try
            {
                timeLeft -= 10000;

                #region Conditions
                if (Session == null)
                { stopTimer(); return; }
                #endregion

                #region Execute
                Session.GetRoomUser().ApplyEffect(0);
                stopTimer();
                #endregion
            }
            catch { stopTimer(); }
        }

        public int getTime()
        {
            int minutesRemaining = timeLeft / 10000;
            return minutesRemaining;
        }

        public void stopTimer()
        {
            timer.Dispose();
        }
    }
}
