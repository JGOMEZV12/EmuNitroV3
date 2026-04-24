using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.Core;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Sendhome timer
    /// </summary>
    public class SendhomeTimer : RoleplayTimer
    {
        public SendhomeTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {
            // Convert to milliseconds
            TimeLeft = base.Client.GetRoleplay().SendHomeTimeLeft * 60000;
        }

        /// <summary>
        /// Pays user after shift
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

                if (base.Client.GetRoleplay().SendHomeTimeLeft <= 0)
                {
                    base.EndTimer();
                    return;
                }

                if (base.Client.GetRoomUser() == null)
                    return;

                if (base.Client.GetRoomUser().IsAsleep)
                    return;

                TimeCount++;
                TimeLeft -= 1000;

                if (TimeCount == 60)
                    base.Client.GetRoleplay().SendHomeTimeLeft--;

                if (TimeLeft > 0)
                {
                    if (TimeCount == 60)
                    {
                        base.Client.SendWhisper("Tienes " + (TimeLeft / 60000) + " Minuto(s) restante hasta que su duración sendhome ha expirado y usted puede trabajar otra vez", 1);
                        TimeCount = 0;

                        if (base.Client.GetRoleplay().SendHomeTimeLeft * 60000 != TimeLeft)
                        {
                            TimeLeft = base.Client.GetRoleplay().SendHomeTimeLeft * 60000;
                            base.Client.SendWhisper("¡La duración de tu sendhome ha cambiado! Ahora tienes " + (TimeLeft / 60000) + " minutos restantes", 1);
                        }
                    }
                    return;
                }

                base.Client.GetRoleplay().SendHomeTimeLeft = 0;
                RoleplayManager.Shout(base.Client, "*Completa su duración de suspensión y puede volver finalmente volver a trabajar*", 4);

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