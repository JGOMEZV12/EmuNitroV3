using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;
using Polar.Core;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Begins healing the users health
    /// </summary>
    public class CurarTimer : RoleplayTimer
    {

        public CurarTimer(string Type, GameClient Client, int Time, bool Forever, object[] Params)
            : base(Type, Client, Time, Forever, Params)
        {
            // Convert to milliseconds
            TimeLeft = 5 * 1000;
        }

        /// <summary>
        /// Decrease our users timer
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

                if (base.Client.GetRoleplay().CurHealth >= base.Client.GetRoleplay().MaxHealth)
                {
                    base.Client.GetRoleplay().CurHealth = base.Client.GetRoleplay().MaxHealth;
                    base.Client.GetRoleplay().BeingHealed = false;
                    base.EndTimer();
                    return;
                }

                if (!base.Client.GetRoleplay().BeingHealed)
                {
                    base.EndTimer();
                    return;
                }

                TimeLeft -= 1000;

                if (TimeLeft > 0)
                    return;

                int NewHealth;

                if (base.Client.GetHabbo().VIPRank > 0)
                    NewHealth = Random.Next(8, 20);
                else
                    NewHealth = Random.Next(5, 16);

                int CurHealth = base.Client.GetRoleplay().CurHealth;
                int MaxHealth = base.Client.GetRoleplay().MaxHealth;

                if (CurHealth + NewHealth < MaxHealth)
                {
                    base.Client.GetRoleplay().BeingHealed = true;
                    base.Client.GetRoleplay().CurHealth += NewHealth;
                    TimeLeft = 5 * 1000;
                    base.Client.SendWhisper("Su salud es ahora " + base.Client.GetRoleplay().CurHealth + "/" + base.Client.GetRoleplay().MaxHealth + "!", 1);
                    return;
                }
                else
                {
                    base.Client.GetRoleplay().BeingHealed = false;
                    base.Client.GetRoleplay().CurHealth = MaxHealth;
                    base.Client.SendWhisper("Su salud está ahora está llena " + base.Client.GetRoleplay().CurHealth + "/" + base.Client.GetRoleplay().MaxHealth + "!", 1);
                    base.EndTimer();
                    return;
                }
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error in Execute() void: " + e);
                base.EndTimer();
            }
        }
    }
}