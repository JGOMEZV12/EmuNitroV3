using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.Core;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Countdown to break handcuffs
    /// </summary>
    public class CuffTimer : RoleplayTimer
    {
        public CuffTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {
            // 8 minutes converted to miliseconds
            TimeLeft = base.Client.GetRoleplay().CuffedTimeLeft * 60000;
            TimeCount = 60 * (8 - base.Client.GetRoleplay().CuffedTimeLeft);
        }

        /// <summary>
        /// Removes the cuff
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

                if (!base.Client.GetRoleplay().Cuffed || base.Client.GetRoleplay().CuffedTimeLeft == 0)
                {
                    base.EndTimer();
                    return;
                }

                TimeCount++;
                TimeLeft -= 1000;

                if (TimeCount == 60 || TimeCount == 60 * 2 || TimeCount == 60 * 3 || TimeCount == 60 * 4 || TimeCount == 60 * 5 || TimeCount == 60 * 6 || TimeCount == 60 * 7)
                {
                    if (TimeCount == 60)
                        base.Client.SendWhisper("Usted comienza a luchar con puños, tratando de liberarse", 1);
                    else if (TimeCount == 60 * 2)
                        base.Client.SendWhisper("¡Las muñecas se lastiman mientras sigues intentando liberarte", 1);
                    else if (TimeCount == 60 * 3)
                        base.Client.SendWhisper("¡Sus muñecas comienzan a  tener moretones!", 1);
                    else if (TimeCount == 60 * 4)
                        base.Client.SendWhisper("Usted gime como el manguito derecho comienza a presionar contra su pulgar derecho!", 1);
                    else if (TimeCount == 60 * 5)
                        base.Client.SendWhisper("¡Escuchas una grieta mientras tu pulgar derecho se disloca!", 1);
                    else if (TimeCount == 60 * 6)
                        base.Client.SendWhisper("¡Desliza tu mano derecha dolorida fuera del manguito y empieza a deslizar el brazalete izquierdo!", 1);
                    else if (TimeCount == 60 * 7)
                        base.Client.SendWhisper("Usted utiliza el clip de papel dentro del puño izquierdo, tratando de obtener la librarse!", 1);

                    base.Client.GetRoleplay().CuffedTimeLeft--;
                }

                if (TimeLeft > 0)
                    return;

                base.Client.GetRoleplay().Cuffed = false;
                base.Client.GetRoleplay().CuffedTimeLeft = 0;
                RoleplayManager.Shout(base.Client, "*¡Se rompne sus esposas después de luchar por tanto tiempo!*", 4);
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